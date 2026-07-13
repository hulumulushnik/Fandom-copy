using Fandom_copy.Data;
using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPostVersionService _versions;

        public PostService(ApplicationDbContext db, IPostVersionService versions)
        {
            _db = db;
            _versions = versions;
        }

        public async Task<ServiceResult<List<PostDto>>> GetAllAsync(Guid? currentUserId, Guid? categoryId = null)
        {
            var query = _db.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Where(p => !p.IsDeleted);

            if (categoryId is not null)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

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
                .Include(p => p.Tags)
                // Loading the whole collection lets EF reconnect every level of
                // the self-referencing section tree, not only one nested level.
                .Include(p => p.Sections)
                .Include(p => p.ContentBlocks)
                    .ThenInclude(b => b.Section)
                .Include(p => p.ContentBlocks)
                    .ThenInclude(b => b.GalleryImages)
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

            var ownerMember = await _db.PostMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.PostId == id && m.Role == PostRole.Owner);

            var author = ownerMember?.User is null ? null : Fandom_copy.DTOs.Profile.AuthorCardDto.FromUser(ownerMember.User);

            return ServiceResult<PostDto>.Ok(PostDto.FromEntity(post, currentRole, author));
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
            await ApplyTagsAsync(post, dto.Tags);

            if (!string.IsNullOrWhiteSpace(post.Description))
            {
                _db.PostContentBlocks.Add(new PostContentBlock
                {
                    Id = Guid.NewGuid(), PostId = post.Id,
                    Type = PostContentBlockType.Text, Text = post.Description, Order = 0
                });
            }

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
                .Include(p => p.Tags)
                .FirstAsync(p => p.Id == post.Id);

            return ServiceResult<PostDto>.Ok(PostDto.FromEntity(created, PostRole.Owner));
        }

        public async Task<ServiceResult<PostDto>> UpdateAsync(Guid id, UpdatePostDto dto, Guid currentUserId)
        {
            var post = await _db.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
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

            var snapshot = await _versions.CaptureAsync(id, currentUserId, "PostUpdated");
            if (!snapshot.Success)
                return ServiceResult<PostDto>.Fail(snapshot.Error ?? "Не вдалося зберегти попередню версію");

            var oldDescription = post.Description;
            var newDescription = dto.Description?.Trim() ?? string.Empty;

            post.Title = dto.Title.Trim();
            post.Description = newDescription;
            post.CategoryId = dto.CategoryId;
            post.IsPublic = dto.IsPublic;
            post.UpdatedAt = DateTime.UtcNow;
            await ApplyTagsAsync(post, dto.Tags);

            var firstRootText = await _db.PostContentBlocks
                .Where(b => b.PostId == post.Id && b.ContainerSectionId == null && b.Type == PostContentBlockType.Text)
                .OrderBy(b => b.Order)
                .FirstOrDefaultAsync();

            if (firstRootText is not null && firstRootText.Text == oldDescription)
            {
                firstRootText.Text = newDescription;
            }
            else if (firstRootText is null && !string.IsNullOrWhiteSpace(newDescription))
            {
                var rootBlocks = await _db.PostContentBlocks
                    .Where(b => b.PostId == post.Id && b.ContainerSectionId == null)
                    .ToListAsync();

                foreach (var block in rootBlocks)
                    block.Order += 1;

                _db.PostContentBlocks.Add(new PostContentBlock
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    ContainerSectionId = null,
                    Type = PostContentBlockType.Text,
                    Text = newDescription,
                    Order = 0
                });
            }

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

            var snapshot = await _versions.CaptureAsync(id, currentUserId, "PostDeleted");
            if (!snapshot.Success)
                return ServiceResult.Fail(snapshot.Error ?? "Не вдалося зберегти попередню версію");

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

        public async Task<ServiceResult> DeleteAsAdminAsync(Guid id, Guid adminUserId)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (post is null)
                return ServiceResult.Fail("Пост не знайдено");

            if (post.IsDeleted)
                return ServiceResult.Fail("Пост уже видалено");

            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;

            _db.PostHistories.Add(new PostHistory
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                UserId = adminUserId,
                Action = "DeletedByAdmin",
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        private async Task ApplyTagsAsync(Post post, List<string>? tagInputs)
        {
            var names = NormalizeTagNames(tagInputs);

            post.Tags.Clear();
            if (names.Count == 0)
                return;

            var loweredNames = names.Select(n => n.ToLower()).ToList();
            var existingTags = await _db.Tags
                .Where(t => loweredNames.Contains(t.Name.ToLower()))
                .ToListAsync();

            foreach (var name in names)
            {
                var tag = existingTags.FirstOrDefault(t =>
                    string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

                if (tag is null)
                {
                    tag = new Tag
                    {
                        Id = Guid.NewGuid(),
                        Name = name
                    };

                    _db.Tags.Add(tag);
                    existingTags.Add(tag);
                }

                post.Tags.Add(tag);
            }
        }

        private static List<string> NormalizeTagNames(List<string>? tagInputs)
        {
            return (tagInputs ?? new List<string>())
                .SelectMany(input => (input ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();
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
