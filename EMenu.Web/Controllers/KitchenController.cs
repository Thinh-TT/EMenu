using EMenu.Application.Services;
using EMenu.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class KitchenController : Controller
    {
        private readonly KitchenService _kitchenService;
        private readonly IHubContext<OrderHub> _hub;

        public KitchenController(KitchenService kitchenService, IHubContext<OrderHub> hub)
        {
            _kitchenService = kitchenService;
            _hub = hub;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
