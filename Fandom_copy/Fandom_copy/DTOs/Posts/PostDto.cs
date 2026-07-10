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
        public List<PostSectionDto> Sections { get; set; } = new();

        public static PostDto FromEntity(Post post)
        {
            return new PostDto
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CategoryId = post.CategoryId,
                CategoryName = post.Category?.Name,
                IsPublic = post.IsPublic,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                Sections = post.Sections
                    .Where(s => s.ParentSectionId == null)
                    .OrderBy(s => s.Order)
                    .Select(PostSectionDto.FromEntity)
                    .ToList()
            };
        }
    }
}
