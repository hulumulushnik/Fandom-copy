using System.ComponentModel.DataAnnotations;

namespace Fandom_copy.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Логін обов'язковий")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Логін має бути від 3 до 32 символів")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обов'язковий")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердження паролю обов'язкове")]
        [Compare(nameof(Password), ErrorMessage = "Паролі не співпадають")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
