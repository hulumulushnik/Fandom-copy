using System.Security.Claims;
using Fandom_copy.DTOs.Auth;
using Fandom_copy.DTOs.Profile;
using Fandom_copy.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fandom_copy.Controllers;

public class AccountController : Controller
{
    private const string ExternalAuthScheme = "ExternalOAuth";

    private readonly IUserService _users;
    private readonly IAuthenticationSchemeProvider _schemes;

    public AccountController(IUserService users, IAuthenticationSchemeProvider schemes)
    {
        _users = users;
        _schemes = schemes;
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Profile));
        ViewData["ReturnUrl"] = returnUrl;
        await SetExternalProvidersAsync();
        return View(new RegisterRequestDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestDto dto, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        await SetExternalProvidersAsync();
        if (!ModelState.IsValid) return View(dto);
        var result = await _users.RegisterAsync(dto);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error!); return View(dto); }
        TempData["Success"] = "Account created. Check your email to confirm it before signing in.";
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Profile));
        ViewData["ReturnUrl"] = returnUrl;
        await SetExternalProvidersAsync();
        return View(new LoginRequestDto());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto dto, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        await SetExternalProvidersAsync();
        if (!ModelState.IsValid) return View(dto);
        var result = await _users.LoginAsync(dto);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error!); return View(dto); }
        await SignInAsync(result.Data!.Id, result.Data.Login, result.Data.GlobalRole.ToString(), dto.RememberMe);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Profile))!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
    {
        if (!await IsExternalProviderAvailableAsync(provider))
        {
            TempData["Error"] = "OAuth provider is not configured.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            TempData["Error"] = $"External provider error: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var externalAuth = await HttpContext.AuthenticateAsync(ExternalAuthScheme);
        if (!externalAuth.Succeeded || externalAuth.Principal is null)
        {
            TempData["Error"] = "External sign-in failed. Please try again.";
            return RedirectToAction(nameof(Login));
        }

        var email = externalAuth.Principal.FindFirstValue(ClaimTypes.Email)
                    ?? externalAuth.Principal.FindFirstValue("email");
        var displayName = externalAuth.Principal.FindFirstValue(ClaimTypes.Name)
                          ?? externalAuth.Principal.FindFirstValue("name")
                          ?? email?.Split('@')[0];

        if (string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync(ExternalAuthScheme);
            TempData["Error"] = "Google/Facebook did not return an email. Allow email access and try again.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _users.ExternalLoginAsync(email, displayName);
        await HttpContext.SignOutAsync(ExternalAuthScheme);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Login));
        }

        await SignInAsync(result.Data!.Id, result.Data.Login, result.Data.GlobalRole.ToString(), remember: false);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Profile))!);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return RedirectToAction(nameof(Login)); }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
    {
        var result = await _users.ConfirmEmailAsync(userId, token);
        ViewData["Confirmed"] = result.Success;
        ViewData["Message"] = result.Success ? "Email confirmed. You can sign in now." : result.Error;
        return View();
    }

    [HttpGet] public IActionResult ResendConfirmation() => View(new ResendConfirmationRequestDto());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequestDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _users.ResendEmailConfirmationAsync(dto);
        TempData["Success"] = "If the account needs confirmation, a new email has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet] public IActionResult ForgotPassword() => View(new ForgotPasswordRequestDto());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _users.RequestPasswordResetAsync(dto);
        TempData["Success"] = "If that email is registered, reset instructions have been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet] public IActionResult ResetPassword(string? email, string? token) => View(new ResetPasswordRequestDto { Email = email ?? "", Token = token ?? "" });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var result = await _users.ResetPasswordAsync(dto);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error!); return View(dto); }
        TempData["Success"] = "Password changed. Sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await _users.GetProfileAsync(CurrentUserId());
        return result.Success ? View(result.Data) : NotFound();
    }
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return await Profile();
        var result = await _users.UpdateProfileAsync(CurrentUserId(), dto);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error!); return await Profile(); }
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpGet] public IActionResult ChangePassword() => View(new ChangePasswordDto());
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var result = await _users.ChangePasswordAsync(CurrentUserId(), dto);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error!); return View(dto); }
        TempData["Success"] = "Password changed.";
        return RedirectToAction(nameof(Profile));
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task SetExternalProvidersAsync()
    {
        ViewData["ExternalProviders"] = (await GetExternalProviderNamesAsync()).ToArray();
    }

    private async Task<bool> IsExternalProviderAvailableAsync(string provider)
    {
        var providers = await GetExternalProviderNamesAsync();
        return providers.Contains(provider, StringComparer.Ordinal);
    }

    private async Task<List<string>> GetExternalProviderNamesAsync()
    {
        var schemes = await _schemes.GetAllSchemesAsync();
        return schemes
            .Where(s => s.Name is "Google" or "Facebook")
            .Select(s => s.Name)
            .ToList();
    }

    private Task SignInAsync(Guid id, string login, string role, bool remember) => HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Name, login), new Claim(ClaimTypes.Role, role) }, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = remember, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(remember ? 30 : 1) });
}
