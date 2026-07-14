using Fandom_copy.DTOs.Profile;
using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Posts
{
    /// <summary>
    /// Один коментар до поста, готовий до відображення у списку коментарів
    /// внизу сторінки поста. Містить мінімальну картку автора коментаря
    /// (аватар, логін, рамка), щоб можна було перейти до його публічного профілю.
    /// </summary>
    public class PostCommentDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public AuthorCardDto Author { get; set; } = new();

        /// <summary>
        /// Чи може поточний користувач видалити цей коментар
        /// (власник коментаря або глобальний адмін).
        /// </summary>
        public bool CanDelete { get; set; }

        public static PostCommentDto FromEntity(PostComment comment, Guid? currentUserId, bool isAdmin)
        {
            return new PostCommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                Author = AuthorCardDto.FromUser(comment.User),
                CanDelete = currentUserId is not null &&
                            (currentUserId.Value == comment.UserId || isAdmin)
            };
        }
    }

    /// <summary>
    /// Форма створення нового коментаря до поста.
    /// </summary>
    public class CreatePostCommentDto
    {
        public string Text { get; set; } = string.Empty;
    }
}
