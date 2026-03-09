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
                int tableId,
                int sessionId)
        {
            ViewBag.TableId = tableId;
            ViewBag.SessionId = sessionId;

            var menu = _service.GetMenu();

            return View(menu);
        }
    }
}
