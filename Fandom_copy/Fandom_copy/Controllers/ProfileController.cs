using System.Security.Claims;
using Fandom_copy.DTOs.Profile;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fandom_copy.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserService _userService;

        public ProfileController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.GetProfileAsync(userId);

            if (!result.Success)
                return NotFound(new { message = result.Error });

            return Ok(result.Data);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _userService.UpdateProfileAsync(userId, dto);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(result.Data);
        }

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _userService.ChangePasswordAsync(userId, dto);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new { message = "Пароль успішно змінено" });
        }

        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(idClaim!);
        }
    }
}