namespace Fandom_copy.Services.Email
{
    /// <summary>
    /// Налаштування SMTP-сервера, прив'язуються з appsettings.json (секція "Smtp").
    /// </summary>
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Fandom";
    }

    /// <summary>
    /// Загальні налаштування застосунку (секція "App"), потрібні для побудови
    /// посилань у листах (підтвердження email, скидання паролю).
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Базова адреса фронтенду, напр. "https://localhost:5173"
        /// </summary>
        public string ClientBaseUrl { get; set; } = string.Empty;
    }
}
