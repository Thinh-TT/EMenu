using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Web.Extensions;
using EMenu.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class WageController : Controller
    {
        private readonly HrService _hrService;
        private readonly StaffService _staffService;
        private readonly ILogger<WageController> _logger;

        public WageController(
            HrService hrService,
            StaffService staffService,
            ILogger<WageController> logger)
        {
            _hrService = hrService;
            _staffService = staffService;
            _logger = logger;
        }

        public IActionResult Index(int? year, int? month)
        {
            var targetYear = year ?? DateTime.Today.Year;
            var targetMonth = month ?? DateTime.Today.Month;

            try
            {
                var vm = new WageIndexViewModel
                {
                    Year = targetYear,
                    Month = targetMonth,
                    Staffs = _staffService.GetAll(),
                    Wages = _hrService.GetAllWages(),
                    MonthlySummary = _hrService.GetMonthlyWageReport(targetYear, targetMonth)
                };

                return View(vm);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed loading wage management for user {UserId} ({Username}) roles {Roles}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles());

                ViewBag.Error = ex.Message;

                return View(new WageIndexViewModel
                {
                    Year = targetYear,
                    Month = targetMonth,
                    Staffs = _staffService.GetAll(),
                    Wages = _hrService.GetAllWages()
                });
            }
        }

        [HttpPost]
        public IActionResult Upsert(int staffId, decimal baseSalary, decimal hourlyRate, int? year, int? month)
        {
            var targetYear = year ?? DateTime.Today.Year;
            var targetMonth = month ?? DateTime.Today.Month;

            try
            {
                var wage = _hrService.UpsertWage(staffId, baseSalary, hourlyRate);

                _logger.LogInformation(
                    "Wage upsert by user {UserId} ({Username}) roles {Roles}: staff {StaffId}, baseSalary {BaseSalary}, hourlyRate {HourlyRate}, wageId {WageId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    staffId,
                    baseSalary,
                    hourlyRate,
                    wage.Id);

                TempData["Success"] = "Wage profile saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Wage upsert failed by user {UserId} ({Username}) roles {Roles}: staff {StaffId}, baseSalary {BaseSalary}, hourlyRate {HourlyRate}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    staffId,
                    baseSalary,
                    hourlyRate);

                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new
            {
                year = targetYear,
                month = targetMonth
            });
        }

        [HttpGet("/api/wage")]
        public IActionResult GetWages()
        {
            var wages = _hrService.GetAllWages()
                .Select(x => new
                {
                    x.Id,
                    x.StaffID,
                    staffName = x.Staff?.StaffName ?? $"Staff {x.StaffID}",
                    x.BaseSalary,
                    x.HourlyRate
                });

            return Ok(wages);
        }

        [HttpPost("/api/wage/upsert")]
        public IActionResult UpsertApi(int staffId, decimal baseSalary, decimal hourlyRate)
        {
            try
            {
                var wage = _hrService.UpsertWage(staffId, baseSalary, hourlyRate);

                _logger.LogInformation(
                    "Wage API upsert by user {UserId} ({Username}) roles {Roles}: staff {StaffId}, baseSalary {BaseSalary}, hourlyRate {HourlyRate}, wageId {WageId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    staffId,
                    baseSalary,
                    hourlyRate,
                    wage.Id);

                return Ok(wage);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Wage API upsert failed by user {UserId} ({Username}) roles {Roles}: staff {StaffId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    staffId);
                return BadRequest(ex.Message);
            }
        }
    }
}
