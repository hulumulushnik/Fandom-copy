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

            var dtos = posts.Select(p => PostDto.FromEntity(p)).ToList();
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

            PostRole? currentRole = null;

            if (currentUserId is not null)
            {
                var member = await _db.PostMembers
                    .FirstOrDefaultAsync(m => m.PostId == id && m.UserId == currentUserId.Value);

                currentRole = member?.Role;
            }

            if (!post.IsPublic && currentRole is null)
                return ServiceResult<PostDto>.Fail("Немає доступу до цього поста");

            return ServiceResult<PostDto>.Ok(PostDto.FromEntity(post, currentRole));
        }

        public async Task<ServiceResult<PostDto>> CreateAsync(CreatePostDto dto, Guid authorId)
        {
            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return ServiceResult<PostDto>.Fail("Категорію не знайдено");

            // Автор має існувати в БД, інакше подальші запити впадуть на FK.
            var authorExists = await _db.Users.AnyAsync(u => u.Id == authorId);
            if (!authorExists)
                return ServiceResult<PostDto>.Fail("Користувача не знайдено");

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

            // Автор одразу стає власником поста — це закриває проблему "будь-хто редагує чужий пост".
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

            return ServiceResult<PostDto>.Ok(PostDto.FromEntity(created, PostRole.Owner));
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

            return ServiceResult<PostDto>.Ok(PostDto.FromEntity(post, member.Role));
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

        // -----------------------------------------------------------------
        //  Members management
        // -----------------------------------------------------------------

        public async Task<ServiceResult<List<PostMemberDto>>> GetMembersAsync(Guid postId, Guid currentUserId)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is null)
                return ServiceResult<List<PostMemberDto>>.Fail("Пост не знайдено");

            var currentMember = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == postId && m.UserId == currentUserId);

            if (currentMember is null || currentMember.Role != PostRole.Owner)
                return ServiceResult<List<PostMemberDto>>.Fail("Лише власник може керувати учасниками");

            var members = await _db.PostMembers
                .Include(m => m.User)
                .Where(m => m.PostId == postId)
                .OrderBy(m => m.Role)
                .ThenBy(m => m.User.Login)
                .ToListAsync();

            return ServiceResult<List<PostMemberDto>>.Ok(members.Select(PostMemberDto.FromEntity).ToList());
        }

        public async Task<ServiceResult<PostMemberDto>> AddMemberAsync(Guid postId, AddPostMemberDto dto, Guid currentUserId)
        {
            if (dto.Role == PostRole.Owner)
                return ServiceResult<PostMemberDto>.Fail("Не можна призначити другого власника");

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
            if (post is null)
                return ServiceResult<PostMemberDto>.Fail("Пост не знайдено");

            var currentMember = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == postId && m.UserId == currentUserId);

            if (currentMember is null || currentMember.Role != PostRole.Owner)
                return ServiceResult<PostMemberDto>.Fail("Лише власник може додавати учасників");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
            if (user is null)
                return ServiceResult<PostMemberDto>.Fail("Користувача не знайдено");

            var existing = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == postId && m.UserId == dto.UserId);

            if (existing is not null)
                return ServiceResult<PostMemberDto>.Fail("Користувач вже є учасником поста");

            var member = new PostMember
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = dto.UserId,
                Role = dto.Role,
                AddedAt = DateTime.UtcNow
            };

            _db.PostMembers.Add(member);

            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = currentUserId,
                Action = $"MemberAdded:{dto.Role}",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            member.User = user;
            return ServiceResult<PostMemberDto>.Ok(PostMemberDto.FromEntity(member));
        }

        public async Task<ServiceResult<PostMemberDto>> UpdateMemberRoleAsync(Guid postId, Guid memberId, UpdatePostMemberRoleDto dto, Guid currentUserId)
        {
            if (dto.Role == PostRole.Owner)
                return ServiceResult<PostMemberDto>.Fail("Роль Owner не можна передати через цей метод");

            var currentMember = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == postId && m.UserId == currentUserId);

            if (currentMember is null || currentMember.Role != PostRole.Owner)
                return ServiceResult<PostMemberDto>.Fail("Лише власник може змінювати ролі");

            var member = await _db.PostMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == memberId && m.PostId == postId);

            if (member is null)
                return ServiceResult<PostMemberDto>.Fail("Учасника не знайдено");

            if (member.Role == PostRole.Owner)
                return ServiceResult<PostMemberDto>.Fail("Не можна змінити роль власника");

            member.Role = dto.Role;

            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = currentUserId,
                Action = $"MemberRoleChanged:{dto.Role}",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return ServiceResult<PostMemberDto>.Ok(PostMemberDto.FromEntity(member));
        }

        public async Task<ServiceResult> RemoveMemberAsync(Guid postId, Guid memberId, Guid currentUserId)
        {
            var currentMember = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == postId && m.UserId == currentUserId);

            if (currentMember is null || currentMember.Role != PostRole.Owner)
                return ServiceResult.Fail("Лише власник може видаляти учасників");

            var member = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.Id == memberId && m.PostId == postId);

            if (member is null)
                return ServiceResult.Fail("Учасника не знайдено");

            if (member.Role == PostRole.Owner)
                return ServiceResult.Fail("Не можна видалити власника поста");

            _db.PostMembers.Remove(member);

            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = currentUserId,
                Action = "MemberRemoved",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<List<UserSearchResultDto>>> SearchUsersAsync(Guid postId, string query, Guid currentUserId, int take = 10)
        {
            var currentMember = await _db.PostMembers
                .FirstOrDefaultAsync(m => m.PostId == postId && m.UserId == currentUserId);

            if (currentMember is null || currentMember.Role != PostRole.Owner)
                return ServiceResult<List<UserSearchResultDto>>.Fail("Лише власник може шукати користувачів для додавання");

            query = (query ?? string.Empty).Trim();
            if (query.Length < 1)
                return ServiceResult<List<UserSearchResultDto>>.Ok(new List<UserSearchResultDto>());

            if (take <= 0) take = 10;
            if (take > 25) take = 25;

            // Виключаємо тих, хто вже є учасником, щоб UI одразу показував тільки релевантних.
            var alreadyMemberIds = await _db.PostMembers
                .Where(m => m.PostId == postId)
                .Select(m => m.UserId)
                .ToListAsync();

            var lowered = query.ToLower();

            var users = await _db.Users
                .Where(u => !u.IsBanned)
                .Where(u => !alreadyMemberIds.Contains(u.Id))
                .Where(u => u.Login.ToLower().Contains(lowered) || u.Email.ToLower().Contains(lowered))
                .OrderBy(u => u.Login)
                .Take(take)
                .Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    Login = u.Login,
                    Email = u.Email
                })
                .ToListAsync();

            return ServiceResult<List<UserSearchResultDto>>.Ok(users);
        }
    }
}
