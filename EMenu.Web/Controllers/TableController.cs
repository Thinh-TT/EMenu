using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Web.Hubs;
using EMenu.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public class TableController : Controller
    {
        private readonly TableService _service;
        private readonly SessionService _sessionService;
        private readonly CheckoutRequestTracker _checkoutRequestTracker;
        private readonly IHubContext<OrderHub> _hub;

        public TableController(
            TableService service,
            SessionService sessionService,
            CheckoutRequestTracker checkoutRequestTracker,
            IHubContext<OrderHub> hub)
        {
            _service = service;
            _sessionService = sessionService;
            _checkoutRequestTracker = checkoutRequestTracker;
            _hub = hub;
        }

        public IActionResult Index()
        {
            var tables = _service.GetAll();
            ViewBag.CheckoutRequests = _checkoutRequestTracker.GetAll();

            return View(tables);
        }

        public IActionResult StartSession(int tableId)
        {
            return RedirectToAction("Start", "Session",
                new { tableId = tableId });
        }

        public async Task<IActionResult> Bill(int tableId)
        {
            var session = _sessionService.GetActiveSessionByTable(tableId);

            if (session == null)
                return RedirectToAction("Index");

            var clearedRequest = _checkoutRequestTracker.ClearByTable(tableId);

            if (clearedRequest != null)
            {
                await _hub.Clients.All.SendAsync("CheckoutRequestCleared", new
                {
                    clearedRequest.SessionId,
                    clearedRequest.TableId,
                    clearedRequest.TableName
                });
            }

            return RedirectToAction("Index", "BillPage",
                new { sessionId = session.OrderSessionID });
        }
    }
}

