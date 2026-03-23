using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class MenuController : Controller
    {
        private readonly MenuService _service;

        public MenuController(MenuService service)
        {
            _service = service;
        }

        public IActionResult Index(
                int? tableId,
                int? sessionId)
        {
            var hasValidSession = sessionId.HasValue && sessionId.Value > 0;

            ViewBag.TableId = tableId;
            ViewBag.SessionId = sessionId;
            ViewBag.HasValidSession = hasValidSession;
            ViewBag.SessionWarning = hasValidSession
                ? null
                : "Session not found! Please start a new order. By scanning the QR code at the table.";

            var menu = _service.GetMenu();

            return View(menu);
        }
    }
}
