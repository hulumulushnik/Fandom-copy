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
    }

    public enum GlobalRole
    {
        User,
        Admin
    }
}
