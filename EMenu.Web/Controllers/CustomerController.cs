using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class CustomerController : Controller
    {
        private readonly SessionService _sessionService;
        private readonly CustomerService _customerService;

        public CustomerController(
            SessionService sessionService,
            CustomerService customerService)
        {
            _sessionService = sessionService;
            _customerService = customerService;
        }

        public IActionResult Start(int tableId)
        {
            ViewBag.TableId = tableId;
            return View();
        }

        [HttpPost]
        public IActionResult Start(
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
