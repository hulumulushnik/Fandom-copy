namespace Fandom_copy.Models
{
    public class Post
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Зв'язки
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public List<PostSection> Sections { get; set; } = new();
        public List<PostMember> Members { get; set; } = new();
        public List<Tag> Tags { get; set; } = new();
        public List<PostHistory> Histories { get; set; } = new();
        //
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPublic { get; set; }
        public bool IsDeleted { get; set; }
    }
}
