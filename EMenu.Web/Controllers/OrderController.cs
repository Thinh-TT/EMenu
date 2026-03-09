using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EMenu.Web.Hubs;
using EMenu.Application.DTOs;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly IHubContext<OrderHub> _hub;
        private readonly SessionService _sessionService;
        public OrderController(
            OrderService orderService,
            SessionService sessionService,
            IHubContext<OrderHub> hub)
        {
            _orderService = orderService;
            _sessionService = sessionService;
            _hub = hub;
        }

        [HttpPost("create")]
        public IActionResult Create(int sessionId, int staffId)
        {
            var order = _orderService.CreateOrder(sessionId, staffId);
            return Ok(order);
        }

        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct(int orderId, int productId, int quantity)
        {
            _orderService.AddProduct(orderId, productId, quantity);

            await _hub.Clients.All.SendAsync("NewOrder", new
            {
                OrderID = orderId,
                ProductID = productId,
                Quantity = quantity
            });

            return Ok();
        }

        [HttpPost("submit")]
        public IActionResult Submit(
    int sessionId,
    [FromBody] List<CartItemDto> items)
        {
            var session =
                _sessionService.GetById(sessionId);

            if (session == null)
                return BadRequest("Session not found");

            foreach (var item in items)
            {
                _orderService.AddProduct(
                    sessionId,
                    item.ProductId,
                    item.Quantity
                );
            }

            return Ok();
        }
    }
}
