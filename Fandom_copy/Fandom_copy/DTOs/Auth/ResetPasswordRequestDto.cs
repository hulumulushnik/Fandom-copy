using System.ComponentModel.DataAnnotations;

namespace Fandom_copy.DTOs.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Токен обов'язковий")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Новий пароль обов'язковий")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердіть новий пароль")]
        [Compare(nameof(NewPassword), ErrorMessage = "Паролі не співпадають")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
