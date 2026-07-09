namespace Fandom_copy.Models
{
    public class PostMember
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;

        public PostRole Role { get; set; } = PostRole.Reader;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public enum PostRole
    {
        Owner,
        Editor,
        Reader
    }
}
