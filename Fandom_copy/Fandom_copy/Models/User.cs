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
    }

    public enum GlobalRole
    {
        User,
        Admin
    }
}
