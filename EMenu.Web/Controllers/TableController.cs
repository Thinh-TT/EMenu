using EMenu.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class TableController : Controller
    {
        private readonly TableService _service;
        private readonly SessionService _sessionService;

        public TableController(
            TableService service,
            SessionService sessionService)
        {
            _service = service;
            _sessionService = sessionService;
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

        public IActionResult Bill(int tableId)
        {
            var session = _sessionService.GetActiveSessionByTable(tableId);

            if (session == null)
                return RedirectToAction("Index");

            return RedirectToAction("Index", "BillPage",
                new { sessionId = session.OrderSessionID });
        }
    }
}
