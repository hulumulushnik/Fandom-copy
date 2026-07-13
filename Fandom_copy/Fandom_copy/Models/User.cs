namespace Fandom_copy.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public GlobalRole GlobalRole { get; set; } = GlobalRole.User;
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public bool IsBanned { get; set; }

        // Підтвердження email
        public bool EmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

        // Відновлення паролю
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        // Кастомізація профілю
        /// <summary>
        /// Іконка (аватар) профілю. Може бути статичним фото або гіфкою.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Фонове зображення профілю. Може бути статичним фото або гіфкою.
        /// </summary>
        public string? BackgroundUrl { get; set; }

        /// <summary>
        /// Декоративна рамка навколо аватара профілю.
        /// </summary>
        public ProfileFrame ProfileFrame { get; set; } = ProfileFrame.None;
    }

    public enum GlobalRole
    {
        User,
        Admin
    }

    /// <summary>
    /// Набір готових декоративних рамок, які користувач може обрати
    /// для свого аватара профілю.
    /// </summary>
    public enum ProfileFrame
    {
        None,
        Gold,
        Neon,
        Fire,
        Ice,
        Royal,
        Galaxy
    }
}
