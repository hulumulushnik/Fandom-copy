using System.ComponentModel.DataAnnotations;

namespace Fandom_copy.DTOs.Profile
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Введіть поточний пароль")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть новий пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердіть новий пароль")]
        [Compare(nameof(NewPassword), ErrorMessage = "Паролі не співпадають")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
