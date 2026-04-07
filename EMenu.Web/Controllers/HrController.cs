using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Domain.Entities;
using EMenu.Web.Extensions;
using EMenu.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public class HrController : Controller
    {
        private readonly HrService _hrService;
        private readonly StaffService _staffService;
        private readonly ILogger<HrController> _logger;

        public HrController(
            HrService hrService,
            StaffService staffService,
            ILogger<HrController> logger)
        {
            _hrService = hrService;
            _staffService = staffService;
            _logger = logger;
        }

        public IActionResult Index(int? staffId, int? year, int? month)
        {
            var targetYear = year ?? DateTime.Today.Year;
            var targetMonth = month ?? DateTime.Today.Month;
            var isAdmin = User.IsInRole(AppRoles.Admin);

            try
            {
                var currentStaffId = TryGetCurrentStaffId();
                var targetStaffId = ResolveTargetStaffId(staffId, isAdmin);
                var records = _hrService.GetTimekeepingByStaffAndMonth(targetStaffId, targetYear, targetMonth);
                var summary = _hrService.GetStaffMonthlyWageReport(targetStaffId, targetYear, targetMonth);

                var vm = new HrIndexViewModel
                {
                    Year = targetYear,
                    Month = targetMonth,
                    IsAdmin = isAdmin,
                    CurrentStaffId = currentStaffId,
                    SelectedStaffId = targetStaffId,
                    Staffs = isAdmin ? _staffService.GetAll() : [],
                    Timekeepings = records,
                    StaffSummary = summary,
                    MonthlySummary = isAdmin
                        ? _hrService.GetMonthlyWageReport(targetYear, targetMonth)
                        : []
                };

                return View(vm);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed loading HR screen for user {UserId} ({Username}) roles {Roles}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles());

                ViewBag.Error = ex.Message;

                return View(new HrIndexViewModel
                {
                    Year = targetYear,
                    Month = targetMonth,
                    IsAdmin = isAdmin,
                    Staffs = isAdmin ? _staffService.GetAll() : []
                });
            }
        }

        [HttpPost]
        public IActionResult CheckIn(int? staffId, int? year, int? month)
        {
            return HandleCheckAction(
                staffId,
                year,
                month,
                actionName: "check-in",
                action: targetStaffId => _hrService.CheckIn(targetStaffId));
        }

        [HttpPost]
        public IActionResult CheckOut(int? staffId, int? year, int? month)
        {
            return HandleCheckAction(
                staffId,
                year,
                month,
                actionName: "check-out",
                action: targetStaffId => _hrService.CheckOut(targetStaffId));
        }

        [HttpGet("/api/hr/timekeeping")]
        public IActionResult GetTimekeeping(int? staffId, int? year, int? month)
        {
            try
            {
                var targetYear = year ?? DateTime.Today.Year;
                var targetMonth = month ?? DateTime.Today.Month;
                var targetStaffId = ResolveTargetStaffId(staffId, User.IsInRole(AppRoles.Admin));
                var records = _hrService.GetTimekeepingByStaffAndMonth(targetStaffId, targetYear, targetMonth);
                var summary = _hrService.GetStaffMonthlyWageReport(targetStaffId, targetYear, targetMonth);

                return Ok(new
                {
                    staffId = targetStaffId,
                    year = targetYear,
                    month = targetMonth,
                    records = records.Select(x => new
                    {
                        x.Id,
                        x.StaffID,
                        date = x.Date.ToString("yyyy-MM-dd"),
                        x.CheckIn,
                        x.CheckOut,
                        workedHours = CalculateWorkedHours(x.CheckIn, x.CheckOut)
                    }),
                    summary
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/api/hr/check-in")]
        public IActionResult CheckInApi(int? staffId)
        {
            try
            {
                var targetStaffId = ResolveTargetStaffId(staffId, User.IsInRole(AppRoles.Admin));
                var record = _hrService.CheckIn(targetStaffId);

                _logger.LogInformation(
                    "HR check-in by user {UserId} ({Username}) roles {Roles}: staff {StaffId}, date {Date}, check-in {CheckIn}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    targetStaffId,
                    record.Date,
                    record.CheckIn);

                return Ok(record);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "HR check-in failed for user {UserId} ({Username}) roles {Roles}, staff {StaffId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    staffId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/api/hr/check-out")]
        public IActionResult CheckOutApi(int? staffId)
        {
            try
            {
                var targetStaffId = ResolveTargetStaffId(staffId, User.IsInRole(AppRoles.Admin));
                var record = _hrService.CheckOut(targetStaffId);

                _logger.LogInformation(
                    "HR check-out by user {UserId} ({Username}) roles {Roles}: staff {StaffId}, date {Date}, check-out {CheckOut}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    targetStaffId,
                    record.Date,
                    record.CheckOut);

                return Ok(record);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "HR check-out failed for user {UserId} ({Username}) roles {Roles}, staff {StaffId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    staffId);
                return BadRequest(ex.Message);
            }
        }

        private IActionResult HandleCheckAction(
            int? staffId,
            int? year,
            int? month,
            string actionName,
            Func<int, Timekeeping> action)
        {
            var targetYear = year ?? DateTime.Today.Year;
            var targetMonth = month ?? DateTime.Today.Month;

            try
            {
                var targetStaffId = ResolveTargetStaffId(staffId, User.IsInRole(AppRoles.Admin));
                var record = action(targetStaffId);

                _logger.LogInformation(
                    "HR {ActionName} by user {UserId} ({Username}) roles {Roles}: staff {StaffId}, date {Date}, check-in {CheckIn}, check-out {CheckOut}.",
                    actionName,
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    targetStaffId,
                    record.Date,
                    record.CheckIn,
                    record.CheckOut);

                TempData["Success"] = $"Successfully {actionName}.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "HR {ActionName} failed for user {UserId} ({Username}) roles {Roles}, staff {StaffId}.",
                    actionName,
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    staffId);
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new
            {
                staffId = User.IsInRole(AppRoles.Admin) ? staffId : null,
                year = targetYear,
                month = targetMonth
            });
        }

        private int ResolveTargetStaffId(int? requestedStaffId, bool isAdmin)
        {
            if (isAdmin)
            {
                if (!requestedStaffId.HasValue)
                {
                    var defaultStaff = _staffService.GetAll().FirstOrDefault();

                    if (defaultStaff == null)
                    {
                        throw new InvalidOperationException("No staff profile found.");
                    }

                    return defaultStaff.StaffID;
                }

                return requestedStaffId.Value;
            }

            var currentStaffId = TryGetCurrentStaffId();

            if (!currentStaffId.HasValue)
            {
                throw new InvalidOperationException("Current user is not linked to a staff profile.");
            }

            return currentStaffId.Value;
        }

        private int? TryGetCurrentStaffId()
        {
            var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdRaw, out var userId))
            {
                return null;
            }

            return _hrService.GetStaffByUserId(userId)?.StaffID;
        }

        private static decimal CalculateWorkedHours(DateTime checkIn, DateTime? checkOut)
        {
            if (!checkOut.HasValue || checkOut.Value <= checkIn)
            {
                return 0m;
            }

            return Math.Round((decimal)(checkOut.Value - checkIn).TotalHours, 2);
        }
    }
}
