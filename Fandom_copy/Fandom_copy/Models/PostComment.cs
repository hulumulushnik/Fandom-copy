namespace Fandom_copy.Models
{
    /// <summary>
    /// Коментар до поста, залишений авторизованим користувачем.
    /// Відображається у самому низу сторінки поста, окремо від
    /// основного вмісту, з можливістю перейти до профілю автора
    /// коментаря по його аватарі.
    /// </summary>
    public class PostComment
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }
        public Post Post { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
