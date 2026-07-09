using System.ComponentModel.DataAnnotations;

namespace Fandom_copy.DTOs.Profile
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Логін обов'язковий")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Логін має бути від 3 до 32 символів")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        public string Email { get; set; } = string.Empty;
    }
}
