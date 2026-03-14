using EMenu.Application.Services;
using EMenu.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = AppRoles.AdminOrStaff)]
[Route("api/[controller]")]
[ApiController]
public class BillController : Controller
{
    private readonly OrderService _orderService;
    private readonly BillService _billService;
    private readonly SessionService _sessionService;
    private readonly PaymentService _paymentService;

    public BillController(
        OrderService orderService,
        BillService billService,
        SessionService sessionService,
        PaymentService paymentService)
    {
        _orderService = orderService;
        _billService = billService;
        _sessionService = sessionService;
        _paymentService = paymentService;
    }

    public IActionResult Index(int sessionId)
    {
        try
        {
            var orderId = _billService.GetOrderIdBySession(sessionId);
            var bill = _billService.GetBillByOrderId(orderId);

            ViewBag.SessionId = sessionId;

            return View(bill);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("table")]
    public IActionResult GetBill(int tableId)
    {
        try
        {
            var session = _sessionService.GetActiveSessionByTable(tableId);

            if (session == null)
                return NotFound("Session not found");

            var bill = _billService.GetBillBySessionId(session.OrderSessionID);

            return Ok(new
            {
                bill.Items,
                Total = bill.TotalAmount
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("checkout")]
    public IActionResult Checkout(int tableId)
    {
        try
        {
            var session = _sessionService.GetActiveSessionByTable(tableId);

            if (session == null)
                return NotFound("Session not found");

            _paymentService.PayCash(session.OrderSessionID);

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
