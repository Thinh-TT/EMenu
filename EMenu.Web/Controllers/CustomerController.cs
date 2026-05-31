using EMenu.Application.Services;
using EMenu.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EMenu.Web.Controllers
{
    public class CustomerController : Controller
    {
        private readonly SessionService _sessionService;
        private readonly CustomerService _customerService;
        private readonly TableService _tableService;
        private readonly IHubContext<OrderHub> _hub;

        public CustomerController(
            SessionService sessionService,
            CustomerService customerService,
            TableService tableService,
            IHubContext<OrderHub> hub)
        {
            _sessionService = sessionService;
            _customerService = customerService;
            _tableService = tableService;
            _hub = hub;
        }

        public IActionResult Start(int tableId)
        {
            var activeSession = _sessionService.GetActiveSessionByTable(tableId);
            if (activeSession != null)
            {
                return Redirect(
                    "/Menu?tableId=" + tableId +
                    "&sessionId=" + activeSession.OrderSessionID
                );
            }

            ViewBag.TableId = tableId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Start(
                 int tableId,
                 string name,
                 string? phone,
                 string? email)
        {
            try
            {
                var customer =
                    _customerService.Create(name, phone, email);

                var session =
                    _sessionService.StartSession(
                        tableId,
                        customer.CustomerID
                    );

                var table = _tableService.GetById(tableId);
                var tableName = table?.TableName ?? $"Table {tableId}";

                await _hub.Clients.All.SendAsync("SessionStarted", new
                {
                    SessionId = session.OrderSessionID,
                    TableId = tableId,
                    TableName = tableName
                });

                return Redirect(
                    "/Menu?tableId=" + tableId +
                    "&sessionId=" + session.OrderSessionID
                );
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.TableId = tableId;
                ViewBag.Error = ex.Message;
                return View();
            }
        }
    }
}
