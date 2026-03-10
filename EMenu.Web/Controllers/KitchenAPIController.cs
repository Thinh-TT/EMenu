using EMenu.Application.Services;
using EMenu.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/kitchen")]
    public class KitchenAPIController : ControllerBase
    {

        private readonly KitchenService _kitchenService;

        public KitchenAPIController(KitchenService kitchenService)
        {
            _kitchenService = kitchenService;
        }

        [HttpGet("pending")]
        public IActionResult GetPending()
        {
            var items = _kitchenService.GetPendingItems();

            return Ok(items);
        }

        [HttpPut("update-status")]
        public IActionResult UpdateStatus(int orderProductId, OrderItemStatus status)
        {
            _kitchenService.UpdateStatus(orderProductId, status);

            return Ok();
        }
    }
}
