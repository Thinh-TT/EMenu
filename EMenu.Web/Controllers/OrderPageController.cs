using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class OrderPageController : Controller
    {
        public IActionResult Tracking(int sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View();
        }
    }
}