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
    private readonly IUserService _users;
    public AccountController(IUserService users) => _users = users;

    [HttpGet] public IActionResult Register() => User.Identity?.IsAuthenticated == true ? RedirectToAction(nameof(Profile)) : View(new RegisterRequestDto());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var result = await _users.RegisterAsync(dto);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error!); return View(dto); }
        TempData["Success"] = "Account created. Check your email to confirm it before signing in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Profile));
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequestDto());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto dto, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(dto);
        var result = await _users.LoginAsync(dto);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error!); return View(dto); }
        await SignInAsync(result.Data!.Id, result.Data.Login, result.Data.GlobalRole.ToString(), dto.RememberMe);
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
    private Task SignInAsync(Guid id, string login, string role, bool remember) => HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Name, login), new Claim(ClaimTypes.Role, role) }, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = remember, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(remember ? 30 : 1) });
}
