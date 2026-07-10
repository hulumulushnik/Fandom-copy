using System.Security.Cryptography;

namespace Fandom_copy.Services.Security
{
    /// <summary>
    /// Генерує криптографічно стійкі, URL-safe токени
    /// (для підтвердження email та відновлення паролю).
    /// </summary>
    public static class TokenGenerator
    {
        public static string Generate(int byteLength = 32)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
