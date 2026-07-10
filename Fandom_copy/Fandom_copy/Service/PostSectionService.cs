using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Services
{
    public class PostSectionService : IPostSectionService
    {
        private readonly ApplicationDbContext _db;

        public PostSectionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<PostSectionDto>> GetByIdAsync(Guid id)
        {
            var section = await _db.PostSections
                .Include(s => s.SubSections)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (section is null)
                return ServiceResult<PostSectionDto>.Fail("Підпост не знайдено");

            return ServiceResult<PostSectionDto>.Ok(PostSectionDto.FromEntity(section));
        }

        public async Task<ServiceResult<List<PostSectionDto>>> GetByPostIdAsync(Guid postId)
        {
            var sections = await _db.PostSections
                .Include(s => s.SubSections)
                .Where(s => s.PostId == postId && s.ParentSectionId == null)
                .OrderBy(s => s.Order)
                .ToListAsync();

            var dtos = sections.Select(PostSectionDto.FromEntity).ToList();
            return ServiceResult<List<PostSectionDto>>.Ok(dtos);
        }

        public async Task<ServiceResult<PostSectionDto>> CreateAsync(CreatePostSectionDto dto, Guid currentUserId)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == dto.PostId && !p.IsDeleted);
            if (post is null)
                return ServiceResult<PostSectionDto>.Fail("Пост не знайдено");

            if (!await CanEditAsync(dto.PostId, currentUserId))
                return ServiceResult<PostSectionDto>.Fail("Немає прав на редагування поста");

            if (dto.ParentSectionId is not null)
            {
                var parent = await _db.PostSections
                    .FirstOrDefaultAsync(s => s.Id == dto.ParentSectionId.Value);

                if (parent is null)
                    return ServiceResult<PostSectionDto>.Fail("Батьківський підпост не знайдено");

                if (parent.PostId != dto.PostId)
                    return ServiceResult<PostSectionDto>.Fail("Батьківський підпост належить іншому посту");
            }

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

            post.UpdatedAt = DateTime.UtcNow;
            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                UserId = currentUserId,
                Action = "SectionCreated",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return ServiceResult<PostSectionDto>.Ok(PostSectionDto.FromEntity(section));
        }

        public async Task<ServiceResult<PostSectionDto>> UpdateAsync(Guid id, UpdatePostSectionDto dto, Guid currentUserId)
        {
            var section = await _db.PostSections.FirstOrDefaultAsync(s => s.Id == id);
            if (section is null)
                return ServiceResult<PostSectionDto>.Fail("Підпост не знайдено");

            if (!await CanEditAsync(section.PostId, currentUserId))
                return ServiceResult<PostSectionDto>.Fail("Немає прав на редагування");

            section.Title = dto.Title.Trim();
            section.Text = dto.Text?.Trim() ?? string.Empty;
            section.Order = dto.Order;

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

            return ServiceResult<PostSectionDto>.Ok(PostSectionDto.FromEntity(section));
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

            _db.PostSections.Remove(section);
        }

        private async Task<bool> CanEditAsync(Guid postId, Guid userId)
        {
            var member = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == postId && m.UserId == userId);

            return member is not null &&
                   (member.Role == PostRole.Owner || member.Role == PostRole.Editor);
        }
    }
}
