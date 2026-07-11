using Fandom_copy.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Fandom_copy.Controllers
{
    public class SettingsController : Controller
    {
        public const string LangCookie = "site_lang";
        public const string ThemeCookie = "site_theme";

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.CurrentLang = UiText.Normalize(Request.Cookies[LangCookie]);
            ViewBag.CurrentTheme = string.IsNullOrWhiteSpace(Request.Cookies[ThemeCookie]) ? "dark" : Request.Cookies[ThemeCookie];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(string lang, string theme, string? returnUrl)
        {
            var normalizedLang = UiText.Normalize(lang);
            var normalizedTheme = UiText.SupportedThemes.Any(t => t.Code == theme) ? theme : "dark";

            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Append(LangCookie, normalizedLang, cookieOptions);
            Response.Cookies.Append(ThemeCookie, normalizedTheme, cookieOptions);

            TempData["Message"] = "saved";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
