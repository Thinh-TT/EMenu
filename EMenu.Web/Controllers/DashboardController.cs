using EMenu.Application.Services;
using EMenu.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : Controller
    {
        private readonly DashboardService _service;

        public DashboardController(DashboardService service)
        {
            _service = service;
        }

        [HttpGet("/Dashboard")]
        [HttpGet("/Dashboard/Index")]
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
            return Ok(_service.GetTodayOrderCount());
        }

        [HttpGet("tables-in-use")]
        public IActionResult TablesInUse()
        {
            return Ok(_service.GetTablesInUseCount());
        }

        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            return Ok(_service.GetExtendedSummary());
        }

        [HttpGet("import-trend")]
        public IActionResult GetImportTrend(int days = 7)
        {
            try
            {
                return Ok(_service.GetImportTrend(days));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("alerts")]
        public IActionResult GetAlerts(int lowStockLimit = 5, int clashLookaheadDays = 7)
        {
            try
            {
                return Ok(_service.GetAlerts(lowStockLimit, clashLookaheadDays));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
