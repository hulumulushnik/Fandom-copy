namespace Fandom_copy.Models
{
    class User
    {
        Guid Id;

        string Login;

        string Email;

        string PasswordHash;

        GlobalRole GlobalRole;

        DateTime RegistrationDate;

        bool IsBanned;
    }

    enum GlobalRole
    {
        User,
        Admin
    }
}
