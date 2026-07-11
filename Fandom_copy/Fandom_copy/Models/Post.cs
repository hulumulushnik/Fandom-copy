namespace Fandom_copy.Models
{
    public class Post
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        /// <summary>
        /// Optional custom icon/thumbnail for the post, set independently from any
        /// images inserted into the post's content. When empty, the UI falls back
        /// to the first content image and then to a letter placeholder.
        /// </summary>
        public string? IconPath { get; set; }
        public List<PostSection> Sections { get; set; } = new();
        public List<PostContentBlock> ContentBlocks { get; set; } = new();
        public List<PostMember> Members { get; set; } = new();
        public List<Tag> Tags { get; set; } = new();
        public List<PostHistory> Histories { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPublic { get; set; }
        public bool IsDeleted { get; set; }
    }
}
