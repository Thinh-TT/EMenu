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

            var activeSessions = new Dictionary<int, int>();
            foreach (var table in tables)
            {
                if (table.Status == 1) // Occupied
                {
                    var session = _sessionService.GetActiveSessionByTable(table.TableID);
                    if (session != null)
                    {
                        activeSessions[table.TableID] = session.OrderSessionID;
                    }
                }
            }
            ViewBag.ActiveSessions = activeSessions;

            // Hardcode area mapping based on table name prefix
            var areaGroups = tables
                .GroupBy(t => GetTableArea(t.TableName))
                .Select(g => new
                {
                    Area = g.Key,
                    Tables = g.OrderBy(t => t.TableName).ToList()
                })
                .ToList();
            ViewBag.AreaGroups = areaGroups;

            return View(tables);
        }

        private static string GetTableArea(string tableName)
        {
            // T01-T04: Open-air area
            // T05-T10: Central area
            if (tableName.StartsWith("T0") && int.TryParse(tableName[1..], out int num))
            {
                if (num >= 1 && num <= 4)
                    return "Open-air area";
                if (num >= 5 && num <= 10)
                    return "Central area";
            }
            return "Other";
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

