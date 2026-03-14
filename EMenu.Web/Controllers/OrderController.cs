using EMenu.Application.DTOs;
using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using EMenu.Web.Extensions;
using EMenu.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly KitchenService _kitchenService;
        private readonly IHubContext<OrderHub> _hub;
        private readonly SessionService _sessionService;
        private readonly AppDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            OrderService orderService,
            KitchenService kitchenService,
            SessionService sessionService,
            IHubContext<OrderHub> hub,
            AppDbContext context,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _kitchenService = kitchenService;
            _sessionService = sessionService;
            _hub = hub;
            _context = context;
            _logger = logger;
        }

        [HttpPost("create")]
        [Authorize(Roles = AppRoles.AdminOrStaff)]
        public IActionResult Create(int sessionId, int staffId)
        {
            try
            {
                var order = _orderService.CreateOrder(sessionId, staffId);

                _logger.LogInformation(
                    "Order created by user {UserId} ({Username}) roles {Roles}: order {OrderId}, session {SessionId}, staff {StaffId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    order.OrderID,
                    sessionId,
                    staffId);
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Order creation failed for user {UserId} ({Username}) roles {Roles}: session {SessionId}, staff {StaffId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId,
                    staffId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("add-product")]
        [Authorize(Roles = AppRoles.AdminOrStaff)]
        public async Task<IActionResult> AddProduct(int sessionId, int productId, int quantity)
        {
            try
            {
                _orderService.AddProduct(sessionId, productId, quantity);

                _logger.LogInformation(
                    "Product added to order by user {UserId} ({Username}) roles {Roles}: session {SessionId}, product {ProductId}, quantity {Quantity}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId,
                    productId,
                    quantity);

                await _hub.Clients.All.SendAsync("NewOrder", new
                {
                    SessionID = sessionId,
                    ProductID = productId,
                    Quantity = quantity
                });

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Add product failed for user {UserId} ({Username}) roles {Roles}: session {SessionId}, product {ProductId}, quantity {Quantity}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId,
                    productId,
                    quantity);
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Add product rejected for user {UserId} ({Username}) roles {Roles}: session {SessionId}, product {ProductId}, quantity {Quantity}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId,
                    productId,
                    quantity);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("submit")]
        [AllowAnonymous]
        public async Task<IActionResult> Submit(int sessionId, [FromBody] List<CartItemDto> items)
        {
            try
            {
                var session = _sessionService.GetById(sessionId);

                if (session == null)
                    return BadRequest("Session not found");

                _orderService.SubmitOrder(sessionId, items);

                var tableName = _context.RestaurantTables
                    .Where(x => x.TableID == session.TableID)
                    .Select(x => x.TableName)
                    .FirstOrDefault();

                await _hub.Clients.All.SendAsync("OrderSubmitted", new
                {
                    SessionID = sessionId,
                    TableID = session.TableID,
                    TableName = tableName ?? $"Table {session.TableID}",
                    ItemCount = items?.Sum(x => x.Quantity) ?? 0,
                    SubmittedAt = DateTime.UtcNow
                });

                _logger.LogInformation(
                    "Order submitted by user {UserId} ({Username}) roles {Roles}: session {SessionId}, table {TableId}, item count {ItemCount}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId,
                    session.TableID,
                    items?.Sum(x => x.Quantity) ?? 0);

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Order submission failed for user {UserId} ({Username}) roles {Roles}: session {SessionId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId);
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Order submission rejected for user {UserId} ({Username}) roles {Roles}: session {SessionId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    sessionId);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("status")]
        [AllowAnonymous]
        public IActionResult GetStatus(int sessionId)
        {
            var items = _context.OrderProducts
                .Where(x => x.Order.OrderSessionID == sessionId)
                .OrderBy(x => x.OrderProductID)
                .Select(x => new
                {
                    x.Product.ProductName,
                    x.Quantity,
                    x.Status
                })
                .ToList();

            return Ok(items);
        }

        [HttpPost("updateStatus")]
        [Authorize(Roles = AppRoles.AdminStaffKitchen)]
        public async Task<IActionResult> UpdateStatus(int orderProductId, int status)
        {
            try
            {
                _kitchenService.UpdateStatus(orderProductId, (OrderItemStatus)status);

                _logger.LogInformation(
                    "Order item status updated by user {UserId} ({Username}) roles {Roles}: order item {OrderProductId}, status {Status}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    orderProductId,
                    status);

                await _hub.Clients.All.SendAsync(
                    "OrderStatusUpdated",
                    orderProductId,
                    status
                );

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Order item status update failed for user {UserId} ({Username}) roles {Roles}: order item {OrderProductId}, status {Status}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    orderProductId,
                    status);
                return BadRequest(ex.Message);
            }
        }
    }
}
