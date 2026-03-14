using EMenu.Application.Services;
using EMenu.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
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
            try
            {
                var bill = _billService.GetBillBySessionId(sessionId);

                ViewBag.SessionId = sessionId;

                return View(bill);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Table");
            }
        }

        public IActionResult Print(int sessionId)
        {
            try
            {
                var bill = _orderService.GetSessionBill(sessionId);

                return View(bill);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Table");
            }
        }
    }
}
