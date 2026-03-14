using EMenu.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public class CheckoutController : Controller
    {
        public IActionResult Index(int sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View();
        }
    }
}
