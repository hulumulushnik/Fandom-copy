using System.Security.Claims;
using Fandom_copy.DTOs.Auth;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Fandom_copy.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.RegisterAsync(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Error });

            await SignInAsync(result.Data!.Id, result.Data.Login, result.Data.GlobalRole.ToString(), rememberMe: false);

            return Ok(new
            {
                message = "Реєстрація успішна",
                user = new { result.Data.Id, result.Data.Login, result.Data.Email, result.Data.GlobalRole }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.LoginAsync(dto);
            if (!result.Success)
                return Unauthorized(new { message = result.Error });

            await SignInAsync(result.Data!.Id, result.Data.Login, result.Data.GlobalRole.ToString(), dto.RememberMe);

            return Ok(new
            {
                message = "Вхід виконано",
                user = new { result.Data.Id, result.Data.Login, result.Data.Email, result.Data.GlobalRole }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Вихід виконано" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _userService.RequestPasswordResetAsync(dto);

            return Ok(new { message = "Якщо такий email зареєстровано, на нього надіслано інструкції з відновлення" });
        }

        private async Task SignInAsync(Guid userId, string login, string role, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, login),
                new(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 30 : 1)
                });
        }
    }
}