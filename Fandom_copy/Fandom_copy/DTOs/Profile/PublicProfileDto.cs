using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Profile
{
    /// <summary>
    /// Публічна (переглядувана іншими) картка профілю користувача.
    /// Використовується на сторінці профілю користувача та в картці
    /// автора, яка прикріплюється до поста.
    /// </summary>
    public class PublicProfileDto
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string GlobalRole { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public string? AvatarUrl { get; set; }
        public string? BackgroundUrl { get; set; }
        public ProfileFrame ProfileFrame { get; set; }
        public string ProfileFrameName => ProfileFrame.ToString();
        public List<PostDto> PublicPosts { get; set; } = new();

        public static PublicProfileDto FromUser(User user, List<PostDto>? publicPosts = null)
        {
            return new PublicProfileDto
            {
                Id = user.Id,
                Login = user.Login,
                GlobalRole = user.GlobalRole.ToString(),
                RegistrationDate = user.RegistrationDate,
                AvatarUrl = user.AvatarUrl,
                BackgroundUrl = user.BackgroundUrl,
                ProfileFrame = user.ProfileFrame,
                PublicPosts = publicPosts ?? new List<PostDto>()
            };
        }
    }

    /// <summary>
    /// Мінімальна картка автора для прикріплення до поста (щоб не тягнути
    /// весь публічний профіль там, де достатньо аватара, рамки та логіна).
    /// </summary>
    public class AuthorCardDto
    {
        public Guid UserId { get; set; }
        public string Login { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public ProfileFrame ProfileFrame { get; set; }
        public string ProfileFrameName => ProfileFrame.ToString();

        public static AuthorCardDto FromUser(User user)
        {
            return new AuthorCardDto
            {
                UserId = user.Id,
                Login = user.Login,
                AvatarUrl = user.AvatarUrl,
                ProfileFrame = user.ProfileFrame
            };
        }
    }

    public class SetProfileFrameDto
    {
        public ProfileFrame Frame { get; set; } = ProfileFrame.None;
    }
}
