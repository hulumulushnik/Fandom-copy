using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Fandom_copy.Services.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public Task SendEmailConfirmationAsync(string toEmail, string login, string confirmationLink)
        {
            var subject = "Підтвердження email — Fandom";
            var body =
                $"<p>Привіт, {login}!</p>" +
                $"<p>Дякуємо за реєстрацію на Fandom. Щоб підтвердити свою пошту, перейди за посиланням:</p>" +
                $"<p><a href=\"{confirmationLink}\">{confirmationLink}</a></p>" +
                $"<p>Посилання дійсне протягом 24 годин. Якщо це були не ви — просто проігноруйте цей лист.</p>";

            return SendAsync(toEmail, subject, body);
        }

        public Task SendPasswordResetAsync(string toEmail, string login, string resetLink)
        {
            var subject = "Відновлення паролю — Fandom";
            var body =
                $"<p>Привіт, {login}!</p>" +
                $"<p>Ми отримали запит на відновлення паролю для твого акаунту. Щоб встановити новий пароль, перейди за посиланням:</p>" +
                $"<p><a href=\"{resetLink}\">{resetLink}</a></p>" +
                $"<p>Посилання дійсне протягом 1 години. Якщо це були не ви — просто проігноруйте цей лист, пароль залишиться незмінним.</p>";

            return SendAsync(toEmail, subject, body);
        }

        private async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            // Якщо SMTP не налаштований (локальна розробка) — не падаємо, а пишемо лист у лог,
            // щоб фронтенд/бек можна було тестувати без реального поштового сервера.
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                _logger.LogWarning(
                    "Smtp не налаштовано (appsettings -> Smtp:Host порожній). Лист НЕ надіслано.\nTo: {To}\nSubject: {Subject}\nBody:\n{Body}",
                    toEmail, subject, htmlBody);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                var socketOptions = _settings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

                await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions);

                if (!string.IsNullOrWhiteSpace(_settings.User))
                    await client.AuthenticateAsync(_settings.User, _settings.Password);

                await client.SendAsync(message);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }
        }
    }
}
