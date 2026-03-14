using EMenu.Application.DTOs;
using EMenu.Application.Services;
using EMenu.Infrastructure.Data;
using EMenu.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : Controller
    {

        private readonly OrderService _orderService;
        private readonly IHubContext<OrderHub> _hub;
        private readonly SessionService _sessionService;
        private readonly AppDbContext _context;

        public OrderController( OrderService orderService, SessionService sessionService, IHubContext<OrderHub> hub, AppDbContext context)
        {
            _orderService = orderService;
            _sessionService = sessionService;
            _hub = hub;
            _context = context;
        }

        [HttpPost("create")]
        public IActionResult Create(int sessionId, int staffId)
        {
            var order = _orderService.CreateOrder(sessionId, staffId);
            return Ok(order);
        }

        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct(int sessionId, int productId, int quantity)
        {
            _orderService.AddProduct(sessionId, productId, quantity);

            await _hub.Clients.All.SendAsync("NewOrder", new
            {
                SessionID = sessionId,
                ProductID = productId,
                Quantity = quantity
            });

            return Ok();
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            int sessionId,
            [FromBody] List<CartItemDto> items)
        {
            var session = _sessionService.GetById(sessionId);

            if (session == null)
                return BadRequest("Session not found");

            if (items == null || items.Count == 0)
                return BadRequest("Cart is empty");

            foreach (var item in items)
            {
                _orderService.AddProduct(
                    sessionId,
                    item.ProductId,
                    item.Quantity
                );
            }

            var tableName = _context.RestaurantTables
                .Where(x => x.TableID == session.TableID)
                .Select(x => x.TableName)
                .FirstOrDefault();

            await _hub.Clients.All.SendAsync("OrderSubmitted", new
            {
                SessionID = sessionId,
                TableID = session.TableID,
                TableName = tableName ?? $"Table {session.TableID}",
                ItemCount = items.Sum(x => x.Quantity),
                SubmittedAt = DateTime.UtcNow
            });

            return Ok();
        }

        [HttpGet("status")]
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
        public async Task<IActionResult> UpdateStatus(int orderProductId, int status)
        {
            var item = _context.OrderProducts.Find(orderProductId);

            if (item == null)
                return NotFound();

            item.Status = (Domain.Enums.OrderItemStatus)status;

            _context.SaveChanges();

            await _hub.Clients.All.SendAsync(
                "OrderStatusUpdated",
                item.OrderProductID,
                status
            );

            return Ok();
        }

    }
}
