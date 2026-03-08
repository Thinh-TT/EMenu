using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class TableController : Controller
    {
        private readonly TableService _service;

        public TableController(TableService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var tables = _service.GetAll();

            return View(tables);
        }

        public IActionResult StartSession(int tableId)
        {
            return RedirectToAction("Start", "Session",
                new { tableId = tableId });
        }
    }
}
