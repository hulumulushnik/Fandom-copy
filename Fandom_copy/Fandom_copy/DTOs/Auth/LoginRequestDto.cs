using System.ComponentModel.DataAnnotations;

namespace Fandom_copy.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Введіть логін або email")]
        public string LoginOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}
