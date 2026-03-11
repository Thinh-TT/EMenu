using EMenu.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EMenu.Infrastructure.Data;


namespace EMenu.Web.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : Controller
    {
        private readonly DashboardService _service;
        private readonly AppDbContext _context;



        public DashboardController(DashboardService service, AppDbContext context)
        {
            _service = service;
            _context = context;
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

        [HttpGet("orders-today")]
        public IActionResult OrdersToday()
        {
            var today = DateTime.Today;

            var count = _context.Orders
                .Where(o => o.CreatedTime.Date == today)
                .Count();

            return Ok(count);
        }

        [HttpGet("tables-in-use")]
        public IActionResult TablesInUse()
        {
            var count = _context.RestaurantTables
                .Where(t => t.Status == 1)
                .Count();

            return Ok(count);
        }

    }
}
