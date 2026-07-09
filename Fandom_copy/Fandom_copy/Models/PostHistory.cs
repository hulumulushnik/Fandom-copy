namespace Fandom_copy.Models
{
    public class PostHistory
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty;

        // Зв'язок
        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

    }
}
