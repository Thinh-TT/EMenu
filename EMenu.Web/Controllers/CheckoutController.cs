using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class CheckoutController : Controller
    {
        public IActionResult Index(int sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View();
        }
    }
}
