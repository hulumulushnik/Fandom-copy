using Fandom_copy.DTOs.Profile;
using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Posts
{
    public class PostDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? IconPath { get; set; }
        public List<string> TagNames { get; set; } = new();
        public List<PostSectionDto> Sections { get; set; } = new();
        public List<PostContentBlockDto> ContentBlocks { get; set; } = new();

        /// <summary>
        /// Картка автора (власника) поста — аватар, рамка та логін.
        /// Прикріплюється до поста, щоб можна було відкрити повний
        /// профіль автора. <c>null</c>, якщо власника не вдалося визначити.
        /// </summary>
        public AuthorCardDto? Author { get; set; }

        /// <summary>
        /// Role of the current viewer for this post, or <c>null</c> if the viewer is
        /// anonymous or not a member of the post.
        /// </summary>
        public PostRole? CurrentUserRole { get; set; }

        public bool CanEdit =>
            CurrentUserRole == PostRole.Owner || CurrentUserRole == PostRole.Editor;

        public bool CanManageMembers => CurrentUserRole == PostRole.Owner;

        public static PostDto FromEntity(Post post, PostRole? currentUserRole = null, AuthorCardDto? author = null)
        {
            var dto = new PostDto
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CategoryId = post.CategoryId,
                CategoryName = post.Category?.Name,
                IsPublic = post.IsPublic,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IconPath = post.IconPath,
                TagNames = post.Tags
                    .OrderBy(t => t.Name)
                    .Select(t => t.Name)
                    .ToList(),
                CurrentUserRole = currentUserRole,
                Author = author,
                Sections = post.Sections
                    .Where(s => s.ParentSectionId == null)
                    .OrderBy(s => s.Order)
                    .Select(s => PostSectionDto.FromEntity(s, includeSubSections: false))
                    .ToList(),
                ContentBlocks = post.ContentBlocks
                    .Where(b => b.ContainerSectionId == null)
                    .OrderBy(b => b.Order)
                    .Select(PostContentBlockDto.FromEntity)
                    .ToList()
            };

            foreach (var section in dto.Sections)
            {
                section.PrimaryImagePath = post.ContentBlocks
                    .Where(b => b.ContainerSectionId == section.Id && b.Type == PostContentBlockType.Image)
                    .OrderBy(b => b.Order)
                    .Select(b => b.ImagePath)
                    .FirstOrDefault();
            }

            return dto;
        }
    }
}
