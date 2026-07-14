using System.Security.Claims;
using Fandom_copy.Data;
using Fandom_copy.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fandom_copy.Controllers;

[Authorize]
[Route("dev-tools")]
public class DevToolsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DevToolsController(
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _db = db;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpPost("grant-admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantAdmin()
    {
        if (!ConsoleAdminEnabled())
        {
            return NotFound();
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        user.GlobalRole = GlobalRole.Admin;
        await _db.SaveChangesAsync();

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, GlobalRole.Admin.ToString())
            }, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
            });

        return Ok(new
        {
            message = "Admin role granted. Reloading the page will show the admin panel.",
            adminUrl = Url.Action("Index", "Admin")
        });
    }

    private bool ConsoleAdminEnabled() =>
        string.Equals(_environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase)
        && _configuration.GetValue<bool>("DevTools:EnableConsoleAdmin");
}
