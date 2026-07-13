using Fandom_copy.DTOs.Posts;

namespace Fandom_copy.DTOs.Categories
{
    /// <summary>
    /// The "/categories/{id}" page: the category's own info plus the posts
    /// inside it that the current viewer (anonymous or signed-in) is allowed to see.
    /// </summary>
    public class CategoryDetailsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<PostDto> Posts { get; set; } = new();
    }
}
