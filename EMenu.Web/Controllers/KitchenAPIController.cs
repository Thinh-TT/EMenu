using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Domain.Enums;
using EMenu.Web.Extensions;
using EMenu.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/kitchen")]
    [Authorize(Roles = AppRoles.AdminStaffKitchen)]
    public class KitchenAPIController : ControllerBase
    {
        private readonly KitchenService _kitchenService;
        private readonly InventoryService _inventoryService;
        private readonly IHubContext<OrderHub> _hub;
        private readonly ILogger<KitchenAPIController> _logger;

        public KitchenAPIController(
            KitchenService kitchenService,
            InventoryService inventoryService,
            IHubContext<OrderHub> hub,
            ILogger<KitchenAPIController> logger)
        {
            _kitchenService = kitchenService;
            _inventoryService = inventoryService;
            _hub = hub;
            _logger = logger;
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
            try
            {
                var justServed = _kitchenService.UpdateStatus(orderProductId, status);

                if (justServed)
                {
                    _inventoryService.DeductStockForServedOrderItem(orderProductId);
                }

                _logger.LogInformation(
                    "Kitchen status updated by user {UserId} ({Username}) roles {Roles}: order item {OrderProductId}, status {Status}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    orderProductId,
                    status);

                await _hub.Clients.All.SendAsync(
                    "OrderStatusUpdated",
                    orderProductId,
                    (int)status
                );

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Kitchen status update failed for user {UserId} ({Username}) roles {Roles}: order item {OrderProductId}, status {Status}.",
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
