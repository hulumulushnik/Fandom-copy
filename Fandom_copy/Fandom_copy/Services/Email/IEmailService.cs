namespace Fandom_copy.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string toEmail, string login, string confirmationLink);
        Task SendPasswordResetAsync(string toEmail, string login, string resetLink);
    }
}
