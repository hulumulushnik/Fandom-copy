namespace Fandom_copy.Models
{
    /// <summary>
    /// A post bookmarked ("saved") by a user for quick access from the
    /// "Saved" section of the sidebar / Jump Back In on the home page.
    /// </summary>
    public class SavedPost
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
