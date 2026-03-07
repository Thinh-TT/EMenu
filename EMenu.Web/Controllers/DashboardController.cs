using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : Controller
    {
        private readonly DashboardService _service;

        public DashboardController(DashboardService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/api/dashboard/revenue")]
        public IActionResult GetRevenue()
        {
            return Ok(_service.GetTodayRevenue());
        }

        [HttpGet("/api/dashboard/top-products")]
        public IActionResult GetTopProducts()
        {
            return Ok(_service.GetTopProducts());
        }

        [HttpGet("/api/dashboard/tables")]
        public IActionResult GetTables()
        {
            return Ok(_service.GetTableStatus());
        }
    }
}
