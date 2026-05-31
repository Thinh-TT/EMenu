using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Web.Extensions;
using EMenu.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

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
                var clientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    sessionId,
                    orderId,
                    bill.TotalAmount,
                    clientIpAddress);

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
        [HttpGet("/payment/vnpay")]
        public IActionResult VNPayPublic(int sessionId)
        {
            try
            {
                var orderId = _billService.GetOrderIdBySession(sessionId);
                var bill = _billService.GetBillByOrderId(orderId);
                var clientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    sessionId,
                    orderId,
                    bill.TotalAmount,
                    clientIpAddress);

                _logger.LogInformation(
                    "VNPay payment initialized publicly: session {SessionId}, order {OrderId}, amount {Amount}.",
                    sessionId,
                    orderId,
                    bill.TotalAmount);

                return Redirect(paymentUrl);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "VNPay public payment initialization failed: session {SessionId}.",
                    sessionId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Tracking", "OrderPage", new { sessionId });
            }
        }

        [AllowAnonymous]
        public IActionResult VNPayReturn()
        {
            var returnData = BuildReturnData(Request.Query);

            if (!_vnPayService.TryParseTxnRef(returnData.TxnRef, out var sessionId, out var orderId))
            {
                _logger.LogWarning(
                    "VNPay return rejected because TxnRef is invalid: {TxnRef}.",
                    returnData.TxnRef);

                return View("PaymentFail", BuildResult(
                    isSuccess: false,
                    message: "Invalid VNPay transaction reference.",
                    returnData,
                    null,
                    null));
            }

            var signedParameters = ExtractSignParameters(Request.Query);
            var validSignature = _vnPayService.ValidateSignature(signedParameters, returnData.SecureHash);

            if (!validSignature)
            {
                _logger.LogWarning(
                    "VNPay return rejected due to invalid signature: txnRef {TxnRef}, session {SessionId}, order {OrderId}.",
                    returnData.TxnRef,
                    sessionId,
                    orderId);

                return View("PaymentFail", BuildResult(
                    isSuccess: false,
                    message: "VNPay signature validation failed.",
                    returnData,
                    sessionId,
                    orderId));
            }

            var isSuccessfulTransaction = VNPayService.IsSuccessfulTransaction(
                returnData.ResponseCode,
                returnData.TransactionStatus);

            if (!isSuccessfulTransaction)
            {
                _logger.LogWarning(
                    "VNPay return failed: txnRef {TxnRef}, session {SessionId}, order {OrderId}, responseCode {ResponseCode}, transactionStatus {TransactionStatus}.",
                    returnData.TxnRef,
                    sessionId,
                    orderId,
                    returnData.ResponseCode,
                    returnData.TransactionStatus);

                return View("PaymentFail", BuildResult(
                    isSuccess: false,
                    message: "VNPay reported an unsuccessful transaction.",
                    returnData,
                    sessionId,
                    orderId));
            }

            try
            {
                _paymentService.PaymentSuccess(orderId);

                _logger.LogInformation(
                    "VNPay return success: txnRef {TxnRef}, session {SessionId}, order {OrderId}, transactionNo {TransactionNo}.",
                    returnData.TxnRef,
                    sessionId,
                    orderId,
                    returnData.TransactionNo ?? string.Empty);

                return View("PaymentSuccess", BuildResult(
                    isSuccess: true,
                    message: "VNPay payment completed successfully.",
                    returnData,
                    sessionId,
                    orderId));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "VNPay return could not finalize payment: txnRef {TxnRef}, session {SessionId}, order {OrderId}.",
                    returnData.TxnRef,
                    sessionId,
                    orderId);

                return View("PaymentFail", BuildResult(
                    isSuccess: false,
                    message: ex.Message,
                    returnData,
                    sessionId,
                    orderId));
            }
        }

        private static VNPayReturnData BuildReturnData(IQueryCollection query)
        {
            return new VNPayReturnData
            {
                TxnRef = query["vnp_TxnRef"].ToString(),
                ResponseCode = query["vnp_ResponseCode"].ToString(),
                TransactionStatus = query["vnp_TransactionStatus"].ToString(),
                SecureHash = query["vnp_SecureHash"].ToString(),
                TransactionNo = query["vnp_TransactionNo"].ToString(),
                PayDate = query["vnp_PayDate"].ToString()
            };
        }

        private static Dictionary<string, string> ExtractSignParameters(IQueryCollection query)
        {
            return query
                .Where(x =>
                    x.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                    !x.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                    !x.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase) &&
                    !StringValues.IsNullOrEmpty(x.Value))
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.ToString(),
                    StringComparer.Ordinal);
        }

        private static VNPayResultViewModel BuildResult(
            bool isSuccess,
            string message,
            VNPayReturnData returnData,
            int? sessionId,
            int? orderId)
        {
            return new VNPayResultViewModel
            {
                IsSuccess = isSuccess,
                Message = message,
                TxnRef = returnData.TxnRef,
                ResponseCode = returnData.ResponseCode,
                TransactionStatus = returnData.TransactionStatus,
                SessionId = sessionId,
                OrderId = orderId,
                TransactionNo = returnData.TransactionNo,
                PayDate = returnData.PayDate
            };
        }
    }
}
