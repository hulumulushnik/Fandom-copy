using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Services
{
    public class PostSectionService : IPostSectionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPostVersionService _versions;

        public PostSectionService(ApplicationDbContext db, IPostVersionService versions)
        {
            _db = db;
            _versions = versions;
        }

        public async Task<ServiceResult<PostSectionDto>> GetByIdAsync(Guid id, Guid? currentUserId)
        {
            var section = await _db.PostSections
                .Include(s => s.SubSections)
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (section is null)
                return ServiceResult<PostSectionDto>.Fail("Підпост не знайдено");

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == section.PostId && !p.IsDeleted);
            if (post is null)
                return ServiceResult<PostSectionDto>.Fail("Пост не знайдено");

            if (!post.IsPublic)
            {
                if (currentUserId is null)
                    return ServiceResult<PostSectionDto>.Fail("Немає доступу до цього підпоста");

                var isMember = await _db.PostMembers
                    .AnyAsync(m => m.PostId == post.Id && m.UserId == currentUserId.Value);

                if (!isMember)
                    return ServiceResult<PostSectionDto>.Fail("Немає доступу до цього підпоста");
            }

            var dto = PostSectionDto.FromEntity(section, includeSubSections: true);
            var contentBlocks = await _db.PostContentBlocks
                .Include(b => b.Section)
                .Where(b => b.ContainerSectionId == section.Id)
                .OrderBy(b => b.Order)
                .ToListAsync();
            dto.ContentBlocks = contentBlocks.Select(PostContentBlockDto.FromEntity).ToList();
            var childIds = dto.SubSections.Select(s => s.Id).ToList();
            if (childIds.Count > 0)
            {
                var childImages = await _db.PostContentBlocks
                    .Where(b => b.Type == PostContentBlockType.Image && b.ContainerSectionId != null && childIds.Contains(b.ContainerSectionId.Value))
                    .OrderBy(b => b.Order)
                    .Select(b => new { SectionId = b.ContainerSectionId!.Value, b.ImagePath })
                    .ToListAsync();

                foreach (var child in dto.SubSections)
                    child.PrimaryImagePath = childImages.FirstOrDefault(i => i.SectionId == child.Id)?.ImagePath;
            }
            dto.Breadcrumbs = await BuildBreadcrumbsAsync(section);
            return ServiceResult<PostSectionDto>.Ok(dto);
        }

        public async Task<ServiceResult<List<PostSectionDto>>> GetByPostIdAsync(Guid postId)
        {
            var sections = await _db.PostSections
                .Include(s => s.SubSections)
                .Where(s => s.PostId == postId && s.ParentSectionId == null)
                .OrderBy(s => s.Order)
                .ToListAsync();

            var dtos = sections.Select(s => PostSectionDto.FromEntity(s, includeSubSections: false)).ToList();
            return ServiceResult<List<PostSectionDto>>.Ok(dtos);
        }

        public async Task<ServiceResult<PostSectionDto>> CreateAsync(CreatePostSectionDto dto, Guid currentUserId)
        {
            if (dto.PostId == Guid.Empty)
                return ServiceResult<PostSectionDto>.Fail("Не вказано пост");

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == dto.PostId && !p.IsDeleted);
            if (post is null)
                return ServiceResult<PostSectionDto>.Fail("Пост не знайдено");

            if (!await CanEditAsync(dto.PostId, currentUserId))
                return ServiceResult<PostSectionDto>.Fail("Немає прав на редагування поста");

            // Батьківський підпост має існувати і належати тому ж посту.
            // Це основна причина минулих 500-х при "підпості в підпості" — форма клала
            // parent id у query, а сервіс лише "перевіряв існування", але не PostId.
            if (dto.ParentSectionId is not null && dto.ParentSectionId != Guid.Empty)
            {
                var parent = await _db.PostSections
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == dto.ParentSectionId.Value);

                if (parent is null)
                    return ServiceResult<PostSectionDto>.Fail("Батьківський підпост не знайдено");

                if (parent.PostId != dto.PostId)
                    return ServiceResult<PostSectionDto>.Fail("Батьківський підпост належить іншому посту");
            }
            else
            {
                // Гарантуємо null, а не Guid.Empty, який ламатиме FK.
                dto.ParentSectionId = null;
            }

            var snapshot = await _versions.CaptureAsync(dto.PostId, currentUserId, "SectionCreated");
            if (!snapshot.Success)
                return ServiceResult<PostSectionDto>.Fail(snapshot.Error ?? "Не вдалося зберегти попередню версію");

            var section = new PostSection
            {
                Id = Guid.NewGuid(),
                PostId = dto.PostId,
                ParentSectionId = dto.ParentSectionId,
                Title = dto.Title.Trim(),
                Text = dto.Text?.Trim() ?? string.Empty,
                Order = dto.Order
            };

            _db.PostSections.Add(section);

            var nextOrder = await _db.PostContentBlocks
                .Where(b => b.PostId == dto.PostId && b.ContainerSectionId == dto.ParentSectionId)
                .Select(b => (int?)b.Order)
                .MaxAsync() ?? -1;
            _db.PostContentBlocks.Add(new PostContentBlock
            {
                Id = Guid.NewGuid(), PostId = dto.PostId,
                ContainerSectionId = dto.ParentSectionId,
                Type = PostContentBlockType.Section, SectionId = section.Id,
                Order = nextOrder + 1
            });
            if (!string.IsNullOrWhiteSpace(section.Text))
            {
                _db.PostContentBlocks.Add(new PostContentBlock
                {
                    Id = Guid.NewGuid(), PostId = dto.PostId,
                    ContainerSectionId = section.Id, Type = PostContentBlockType.Text,
                    Text = section.Text, Order = 0
                });
            }

            post.UpdatedAt = DateTime.UtcNow;
            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                UserId = currentUserId,
                Action = dto.ParentSectionId is null ? "SectionCreated" : "SubSectionCreated",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return ServiceResult<PostSectionDto>.Ok(PostSectionDto.FromEntity(section, includeSubSections: false));
        }

        public async Task<ServiceResult<PostSectionDto>> UpdateAsync(Guid id, UpdatePostSectionDto dto, Guid currentUserId)
        {
            var section = await _db.PostSections.FirstOrDefaultAsync(s => s.Id == id);
            if (section is null)
                return ServiceResult<PostSectionDto>.Fail("Підпост не знайдено");

            if (!await CanEditAsync(section.PostId, currentUserId))
                return ServiceResult<PostSectionDto>.Fail("Немає прав на редагування");

            var snapshot = await _versions.CaptureAsync(section.PostId, currentUserId, "SectionUpdated");
            if (!snapshot.Success)
                return ServiceResult<PostSectionDto>.Fail(snapshot.Error ?? "Не вдалося зберегти попередню версію");

            var oldText = section.Text;
            var newText = dto.Text?.Trim() ?? string.Empty;

            section.Title = dto.Title.Trim();
            section.Text = newText;
            section.Order = dto.Order;

            var firstTextBlock = await _db.PostContentBlocks
                .Where(b => b.PostId == section.PostId && b.ContainerSectionId == section.Id && b.Type == PostContentBlockType.Text)
                .OrderBy(b => b.Order)
                .FirstOrDefaultAsync();

            if (firstTextBlock is not null && firstTextBlock.Text == oldText)
            {
                firstTextBlock.Text = newText;
            }
            else if (firstTextBlock is null && !string.IsNullOrWhiteSpace(newText))
            {
                var blocks = await _db.PostContentBlocks
                    .Where(b => b.PostId == section.PostId && b.ContainerSectionId == section.Id)
                    .ToListAsync();

                foreach (var block in blocks)
                    block.Order += 1;

                _db.PostContentBlocks.Add(new PostContentBlock
                {
                    Id = Guid.NewGuid(),
                    PostId = section.PostId,
                    ContainerSectionId = section.Id,
                    Type = PostContentBlockType.Text,
                    Text = newText,
                    Order = 0
                });
            }

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == section.PostId);
            if (post is not null)
            {
                post.UpdatedAt = DateTime.UtcNow;
                _db.PostHistories.Add(new PostHistory
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    UserId = currentUserId,
                    Action = "SectionUpdated",
                    Date = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            return ServiceResult<PostSectionDto>.Ok(PostSectionDto.FromEntity(section, includeSubSections: false));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, Guid currentUserId)
        {
            var section = await _db.PostSections
                .Include(s => s.SubSections)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (section is null)
                return ServiceResult.Fail("Підпост не знайдено");

            if (!await CanEditAsync(section.PostId, currentUserId))
                return ServiceResult.Fail("Немає прав на видалення");

            var snapshot = await _versions.CaptureAsync(section.PostId, currentUserId, "SectionDeleted");
            if (!snapshot.Success)
                return ServiceResult.Fail(snapshot.Error ?? "Не вдалося зберегти попередню версію");

            // Каскадне видалення дочірніх підпостів (обмежене на рівні БД, тому робимо руками)
            await DeleteSectionRecursiveAsync(section);

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == section.PostId);
            if (post is not null)
            {
                post.UpdatedAt = DateTime.UtcNow;
                _db.PostHistories.Add(new PostHistory
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    UserId = currentUserId,
                    Action = "SectionDeleted",
                    Date = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        private async Task DeleteSectionRecursiveAsync(PostSection section)
        {
            var children = await _db.PostSections
                .Include(s => s.SubSections)
                .Where(s => s.ParentSectionId == section.Id)
                .ToListAsync();

            foreach (var child in children)
                await DeleteSectionRecursiveAsync(child);

            var blocks = await _db.PostContentBlocks
                .Where(b => b.ContainerSectionId == section.Id || b.SectionId == section.Id)
                .ToListAsync();
            _db.PostContentBlocks.RemoveRange(blocks);
            _db.PostSections.Remove(section);
        }

        private async Task<bool> CanEditAsync(Guid postId, Guid userId)
        {
            var member = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == postId && m.UserId == userId);

            return member is not null &&
                   (member.Role == PostRole.Owner || member.Role == PostRole.Editor);
        }

        private async Task<List<PostSectionBreadcrumbDto>> BuildBreadcrumbsAsync(PostSection section)
        {
            var breadcrumbs = new List<PostSectionBreadcrumbDto>();
            var currentParentId = section.ParentSectionId;

            // Обмежуємо глибину на випадок пошкоджених даних (цикли).
            var guard = 32;
            while (currentParentId is not null && guard-- > 0)
            {
                var parent = await _db.PostSections
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == currentParentId.Value);

                if (parent is null) break;

                breadcrumbs.Insert(0, new PostSectionBreadcrumbDto
                {
                    Id = parent.Id,
                    Title = parent.Title
                });

                currentParentId = parent.ParentSectionId;
            }

            return breadcrumbs;
        }
    }
}
