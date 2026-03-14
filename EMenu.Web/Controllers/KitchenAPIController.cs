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
        private readonly IHubContext<OrderHub> _hub;
        private readonly ILogger<KitchenAPIController> _logger;

        public KitchenAPIController(
            KitchenService kitchenService,
            IHubContext<OrderHub> hub,
            ILogger<KitchenAPIController> logger)
        {
            _kitchenService = kitchenService;
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
                _kitchenService.UpdateStatus(orderProductId, status);

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
