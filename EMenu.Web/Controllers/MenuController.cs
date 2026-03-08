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

        public IActionResult Index(int tableId)
        {
            ViewBag.TableId = tableId;

            var menu = _service.GetMenu();

            return View(menu);
        }
    }
}
