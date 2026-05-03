using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [AllowAnonymous]
    public class LocalizationController : Controller
    {
        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                return RedirectToAction("Index", "Home");
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
