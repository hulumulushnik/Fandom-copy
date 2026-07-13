using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Profile
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GlobalRole { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public bool IsBanned { get; set; }
        public bool EmailConfirmed { get; set; }

        // Кастомізація профілю
        public string? AvatarUrl { get; set; }
        public string? BackgroundUrl { get; set; }
        public ProfileFrame ProfileFrame { get; set; }
        public string ProfileFrameName => ProfileFrame.ToString();

        public static UserProfileDto FromUser(User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                Login = user.Login,
                Email = user.Email,
                GlobalRole = user.GlobalRole.ToString(),
                RegistrationDate = user.RegistrationDate,
                IsBanned = user.IsBanned,
                EmailConfirmed = user.EmailConfirmed,
                AvatarUrl = user.AvatarUrl,
                BackgroundUrl = user.BackgroundUrl,
                ProfileFrame = user.ProfileFrame
            };
        }
    }
}
