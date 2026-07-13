namespace Fandom_copy.Models
{
    public class PostVersion
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Action { get; set; } = string.Empty;
        public string SnapshotJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
