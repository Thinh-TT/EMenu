using EMenu.Application.Services;
using EMenu.Domain.Enums;
using EMenu.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/kitchen")]
    public class KitchenAPIController : ControllerBase
    {

        private readonly KitchenService _kitchenService;
        private readonly IHubContext<OrderHub> _hub;

        public KitchenAPIController(
            KitchenService kitchenService,
            IHubContext<OrderHub> hub)
        {
            _kitchenService = kitchenService;
            _hub = hub;
        }

        [HttpGet("pending")]
        public IActionResult GetPending()
        {
            var items = _kitchenService.GetPendingItems();

            return Ok(items);
        }

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus(int orderProductId, OrderItemStatus status)
        {
            _kitchenService.UpdateStatus(orderProductId, status);

            await _hub.Clients.All.SendAsync(
                "OrderStatusUpdated",
                orderProductId,
                (int)status
            );

            return Ok();
        }
    }
}
