using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EMenu.Web.Hubs;

namespace EMenu.Web.Controllers
{
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
