using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class PaymentController : Controller
    {
        private readonly VNPayService _vnPayService;
        private readonly PaymentService _paymentService;
        private readonly BillService _billService;

        public PaymentController(
            VNPayService vnPayService,
            BillService billService,
            PaymentService paymentService)
        {
            _vnPayService = vnPayService;
            _billService = billService;
            _paymentService = paymentService;
        }

        public IActionResult VNPay(int sessionId)
        {
            var orderId = _billService.GetOrderIdBySession(sessionId);

            var bill = _billService.GetBillByOrderId(orderId);

            var paymentUrl =
                _vnPayService.CreatePaymentUrl(
                    sessionId,
                    bill.TotalAmount);

            Console.WriteLine(paymentUrl);
            
            return Redirect(paymentUrl);
        }
        public IActionResult Cash(int sessionId)
        {
            _paymentService.PayCash(sessionId);

            return Redirect("/Table");
        }

        public IActionResult VNPayReturn()
        {
            var responseCode = Request.Query["vnp_ResponseCode"];

            var sessionId = Request.Query["vnp_TxnRef"];

            if (responseCode == "00")
            {
                // thanh toán thành công

                // TODO:
                // create invoice
                // create payment
                // end session

                return View("PaymentSuccess");
            }

            return View("PaymentFail");
        }

    }
}
