using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EMenu.Web.Hubs;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly IHubContext<OrderHub> _hub;

        public OrderController(OrderService orderService,
                       IHubContext<OrderHub> hub)
        {
            _orderService = orderService;
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
            var item = _orderService.AddProduct(orderId, productId, quantity);

            await _hub.Clients.All.SendAsync("NewOrder", new
            {
                item.OrderProductID,
                item.ProductID,
                item.Quantity,
                item.Status
            });

            return Ok(item);
        }
    }
}
