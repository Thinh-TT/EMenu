using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class BillPageController : Controller
    {
        private readonly BillService _billService;
        private readonly OrderService _orderService;

        public BillPageController(BillService billService, OrderService orderService)
        {
            _billService = billService;
            _orderService = orderService;
        }

        public IActionResult Index(int sessionId)
        {
            var bill = _billService.GetBillBySessionId(sessionId);

            ViewBag.SessionId = sessionId;

            return View(bill);
        }

        public IActionResult Print(int sessionId)
        {
            var bill = _orderService.GetSessionBill(sessionId);

            return View(bill);
        }

    }
}
