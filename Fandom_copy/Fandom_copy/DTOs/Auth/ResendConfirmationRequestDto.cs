using System.ComponentModel.DataAnnotations;

namespace Fandom_copy.DTOs.Auth
{
    public class ResendConfirmationRequestDto
    {
        [Required(ErrorMessage = "Email обов'язковий")]
        [EmailAddress(ErrorMessage = "Некоректний email")]
        public string Email { get; set; } = string.Empty;
    }
}
