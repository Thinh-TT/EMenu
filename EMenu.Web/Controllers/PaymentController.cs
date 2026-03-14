using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public class PaymentController : Controller
    {
        private readonly VNPayService _vnPayService;
        private readonly PaymentService _paymentService;
        private readonly BillService _billService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            VNPayService vnPayService,
            BillService billService,
            PaymentService paymentService,
            ILogger<PaymentController> logger)
        {
            _vnPayService = vnPayService;
            _billService = billService;
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult VNPay(int sessionId)
        {
            try
            {
                var orderId = _billService.GetOrderIdBySession(sessionId);
                var bill = _billService.GetBillByOrderId(orderId);

                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    sessionId,
                    bill.TotalAmount);

                _logger.LogInformation(
                    "VNPay payment initialized by user {UserId} ({Username}) roles {Roles}: session {SessionId}, order {OrderId}, amount {Amount}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId,
                    orderId,
                    bill.TotalAmount);

                return Redirect(paymentUrl);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "VNPay payment initialization failed by user {UserId} ({Username}) roles {Roles}: session {SessionId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Checkout", new { sessionId });
            }
        }

        [HttpPost]
        public IActionResult Cash(int sessionId)
        {
            try
            {
                _paymentService.PayCash(sessionId);

                _logger.LogInformation(
                    "Cash payment completed by user {UserId} ({Username}) roles {Roles}: session {SessionId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId);

                TempData["Success"] = "Cash payment completed.";
                return Redirect("/Table");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Cash payment failed by user {UserId} ({Username}) roles {Roles}: session {SessionId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Checkout", new { sessionId });
            }
        }

        [AllowAnonymous]
        public IActionResult VNPayReturn()
        {
            var responseCode = Request.Query["vnp_ResponseCode"];
            var sessionId = Request.Query["vnp_TxnRef"];

            if (responseCode == "00")
            {
                _logger.LogInformation(
                    "VNPay return success for session reference {SessionReference}.",
                    sessionId.ToString());
                return View("PaymentSuccess");
            }

            _logger.LogWarning(
                "VNPay return failed for session reference {SessionReference} with response code {ResponseCode}.",
                sessionId.ToString(),
                responseCode.ToString());
            return View("PaymentFail");
        }
    }
}
