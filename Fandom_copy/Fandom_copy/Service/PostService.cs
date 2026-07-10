using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _db;

        public PostService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<List<PostDto>>> GetAllAsync(Guid? currentUserId)
        {
            var query = _db.Posts
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted);

            if (currentUserId is null)
            {
                query = query.Where(p => p.IsPublic);
            }
            else
            {
                var uid = currentUserId.Value;
                query = query.Where(p =>
                    p.IsPublic ||
                    _db.PostMembers.Any(m => m.PostId == p.Id && m.UserId == uid));
            }

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var dtos = posts.Select(PostDto.FromEntity).ToList();
            return ServiceResult<List<PostDto>>.Ok(dtos);
        }

        public async Task<ServiceResult<PostDto>> GetByIdAsync(Guid id, Guid? currentUserId)
        {
            var post = await _db.Posts
                .Include(p => p.Category)
                // Loading the whole collection lets EF reconnect every level of
                // the self-referencing section tree, not only one nested level.
                .Include(p => p.Sections)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (post is null)
                return ServiceResult<PostDto>.Fail("Пост не знайдено");

            if (!post.IsPublic)
            {
                if (currentUserId is null)
                    return ServiceResult<PostDto>.Fail("Немає доступу до цього поста");

                var isMember = await _db.PostMembers
                    .AnyAsync(m => m.PostId == id && m.UserId == currentUserId.Value);

                if (!isMember)
                    return ServiceResult<PostDto>.Fail("Немає доступу до цього поста");
            }

            return ServiceResult<PostDto>.Ok(PostDto.FromEntity(post));
        }

        public async Task<ServiceResult<PostDto>> CreateAsync(CreatePostDto dto, Guid authorId)
        {
            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return ServiceResult<PostDto>.Fail("Категорію не знайдено");

            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                CategoryId = dto.CategoryId,
                IsPublic = dto.IsPublic,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Posts.Add(post);

            _db.PostMembers.Add(new PostMember
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                UserId = authorId,
                Role = PostRole.Owner,
                AddedAt = DateTime.UtcNow
            });

            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                UserId = authorId,
                Action = "Created",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            var created = await _db.Posts
                .Include(p => p.Category)
                .FirstAsync(p => p.Id == post.Id);

            return ServiceResult<PostDto>.Ok(PostDto.FromEntity(created));
        }

        public async Task<ServiceResult<PostDto>> UpdateAsync(Guid id, UpdatePostDto dto, Guid currentUserId)
        {
            var post = await _db.Posts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (post is null)
                return ServiceResult<PostDto>.Fail("Пост не знайдено");

            var member = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == id && m.UserId == currentUserId);

            if (member is null || (member.Role != PostRole.Owner && member.Role != PostRole.Editor))
                return ServiceResult<PostDto>.Fail("Немає прав на редагування");

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return ServiceResult<PostDto>.Fail("Категорію не знайдено");

            post.Title = dto.Title.Trim();
            post.Description = dto.Description?.Trim() ?? string.Empty;
            post.CategoryId = dto.CategoryId;
            post.IsPublic = dto.IsPublic;
            post.UpdatedAt = DateTime.UtcNow;

            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                UserId = currentUserId,
                Action = "Updated",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return ServiceResult<PostDto>.Ok(PostDto.FromEntity(post));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, Guid currentUserId)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (post is null)
                return ServiceResult.Fail("Пост не знайдено");

            var member = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == id && m.UserId == currentUserId);

            if (member is null || member.Role != PostRole.Owner)
                return ServiceResult.Fail("Тільки власник може видалити пост");

            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;

            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                UserId = currentUserId,
                Action = "Deleted",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return ServiceResult.Ok();
        }
    }
}
