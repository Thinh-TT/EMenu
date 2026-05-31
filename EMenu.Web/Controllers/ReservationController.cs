using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Domain.Enums;
using EMenu.Web.Extensions;
using EMenu.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public class ReservationController : Controller
    {
        private readonly ReservationService _reservationService;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(
            ReservationService reservationService,
            ILogger<ReservationController> logger)
        {
            _reservationService = reservationService;
            _logger = logger;
        }

        public IActionResult Index(DateTime? fromDate, DateTime? toDate, int? tableId, int? status)
        {
            try
            {
                return View(BuildIndexViewModel(fromDate, toDate, tableId, status));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return View(BuildIndexViewModel(null, null, null, null));
            }
        }

        [HttpPost]
        public IActionResult Create(
            string customerName,
            string? customerPhone,
            string? customerEmail,
            int tableId,
            DateTime reservationTime,
            int numberOfGuests,
            DateTime? fromDate,
            DateTime? toDate,
            int? filterTableId,
            int? filterStatus)
        {
            try
            {
                var reservation = _reservationService.CreateReservationForCustomer(
                    customerName,
                    customerPhone,
                    customerEmail,
                    tableId,
                    reservationTime,
                    numberOfGuests);

                _logger.LogInformation(
                    "Reservation created by user {UserId} ({Username}) roles {Roles}: reservation {ReservationId}, table {TableId}, time {ReservationTime}, status {Status}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    reservation.ReservationID,
                    reservation.TableID,
                    reservation.ReservationTime,
                    reservation.Status);

                TempData["Success"] = $"Reservation #{reservation.ReservationID} created successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new
            {
                fromDate,
                toDate,
                tableId = filterTableId,
                status = filterStatus
            });
        }

        [HttpPost]
        public IActionResult Confirm(
            int reservationId,
            DateTime? fromDate,
            DateTime? toDate,
            int? tableId,
            int? status)
        {
            try
            {
                var reservation = _reservationService.ConfirmReservation(reservationId);

                _logger.LogInformation(
                    "Reservation confirmed by user {UserId} ({Username}) roles {Roles}: reservation {ReservationId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    reservation.ReservationID);

                TempData["Success"] = $"Reservation #{reservation.ReservationID} confirmed.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new
            {
                fromDate,
                toDate,
                tableId,
                status
            });
        }

        [HttpPost]
        public IActionResult Cancel(
            int reservationId,
            DateTime? fromDate,
            DateTime? toDate,
            int? tableId,
            int? status)
        {
            try
            {
                var reservation = _reservationService.CancelReservation(reservationId);

                _logger.LogInformation(
                    "Reservation cancelled by user {UserId} ({Username}) roles {Roles}: reservation {ReservationId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    reservation.ReservationID);

                TempData["Success"] = $"Reservation #{reservation.ReservationID} cancelled.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new
            {
                fromDate,
                toDate,
                tableId,
                status
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Book()
        {
            return View(BuildBookViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Book(ReservationBookViewModel model)
        {
            try
            {
                var reservation = _reservationService.CreateReservationForCustomer(
                    model.CustomerName,
                    model.CustomerPhone,
                    model.CustomerEmail,
                    model.TableId ?? 0,
                    model.ReservationTime,
                    model.NumberOfGuests);

                TempData["Success"] =
                    $"Reservation #{reservation.ReservationID} submitted successfully. Current status: {reservation.Status}.";

                return RedirectToAction(nameof(Book));
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.Error = ex.Message;
                return View(BuildBookViewModel(model));
            }
        }

        [HttpGet("/api/reservation")]
        public IActionResult GetReservationsApi(DateTime? fromDate, DateTime? toDate, int? tableId, int? status)
        {
            try
            {
                var normalizedFromDate = (fromDate ?? DateTime.Today).Date;
                var normalizedToDate = (toDate ?? DateTime.Today.AddDays(7)).Date.AddDays(1).AddTicks(-1);

                var reservations = _reservationService.GetReservations(
                    normalizedFromDate,
                    normalizedToDate,
                    tableId,
                    status);

                return Ok(reservations.Select(ToReservationApiResponse));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/api/reservation/create")]
        public IActionResult CreateApi(int customerId, int tableId, DateTime reservationTime, int numberOfGuests)
        {
            try
            {
                var reservation = _reservationService.CreateReservation(
                    customerId,
                    tableId,
                    reservationTime,
                    numberOfGuests);

                return Ok(ToReservationApiResponse(reservation));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/api/reservation/{reservationId:int}/confirm")]
        public IActionResult ConfirmApi(int reservationId)
        {
            try
            {
                var reservation = _reservationService.ConfirmReservation(reservationId);
                return Ok(ToReservationApiResponse(reservation));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/api/reservation/{reservationId:int}/cancel")]
        public IActionResult CancelApi(int reservationId)
        {
            try
            {
                var reservation = _reservationService.CancelReservation(reservationId);
                return Ok(ToReservationApiResponse(reservation));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("/api/reservation/check-conflict")]
        public IActionResult CheckConflictApi(int tableId, DateTime reservationTime, int? ignoredReservationId = null)
        {
            try
            {
                var hasConflict = _reservationService.HasConflict(tableId, reservationTime, ignoredReservationId);

                return Ok(new
                {
                    tableId,
                    reservationTime,
                    hasConflict
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("/api/reservation/available-tables")]
        public IActionResult GetAvailableTables(DateTime reservationTime, int numberOfGuests)
        {
            try
            {
                var tables = _reservationService.GetTables();

                var result = tables.Select(table =>
                {
                    bool hasConflict;
                    try
                    {
                        hasConflict = _reservationService.HasConflict(table.TableID, reservationTime);
                    }
                    catch
                    {
                        hasConflict = true;
                    }

                    bool capacityOk = numberOfGuests <= table.Capacity;
                    bool isAvailable = !hasConflict && capacityOk;

                    string? reason = null;
                    if (!isAvailable)
                    {
                        if (hasConflict)
                            reason = "Đã có đặt bàn trước";
                        else if (!capacityOk)
                            reason = $"Sức chứa tối đa {table.Capacity} khách";
                    }

                    return new
                    {
                        tableId = table.TableID,
                        tableName = table.TableName,
                        capacity = table.Capacity,
                        area = GetTableArea(table.TableName),
                        currentStatus = table.Status,
                        isAvailable,
                        reason
                    };
                })
                .OrderBy(t => t.area)
                .ThenBy(t => t.tableName)
                .ToList();

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static string GetTableArea(string tableName)
        {
            // T01-T04: Open-air area
            // T05-T10: Central area
            if (tableName.StartsWith("T0") && int.TryParse(tableName[1..], out int num))
            {
                if (num >= 1 && num <= 4)
                    return "Open-air area";
                if (num >= 5 && num <= 10)
                    return "Central area";
            }
            return "Other area";
        }

        private ReservationIndexViewModel BuildIndexViewModel(
            DateTime? fromDate,
            DateTime? toDate,
            int? tableId,
            int? status)
        {
            var normalizedFromDate = (fromDate ?? DateTime.Today).Date;
            var normalizedToDate = (toDate ?? DateTime.Today.AddDays(7)).Date;
            var fromDateForQuery = normalizedFromDate;
            var toDateForQuery = normalizedToDate.AddDays(1).AddTicks(-1);
            var tables = _reservationService.GetTables()
                .OrderBy(x => x.TableName)
                .ToList();

            return new ReservationIndexViewModel
            {
                FromDate = normalizedFromDate,
                ToDate = normalizedToDate,
                TableId = tableId,
                Status = status,
                CreateTableId = tableId ?? tables.FirstOrDefault()?.TableID,
                ReservationTime = DateTime.Now.AddHours(1),
                NumberOfGuests = 2,
                Tables = tables,
                Reservations = _reservationService.GetReservations(
                    fromDateForQuery,
                    toDateForQuery,
                    tableId,
                    status)
            };
        }

        private ReservationBookViewModel BuildBookViewModel(ReservationBookViewModel? model = null)
        {
            model ??= new ReservationBookViewModel();
            var tables = _reservationService.GetTables()
                .OrderBy(x => x.TableName)
                .ToList();

            model.TableId ??= tables.FirstOrDefault()?.TableID;

            if (model.ReservationTime <= DateTime.Now)
            {
                model.ReservationTime = DateTime.Now.AddHours(1);
            }

            model.Tables = tables;

            return model;
        }

        private static object ToReservationApiResponse(EMenu.Domain.Entities.Reservation reservation)
        {
            return new
            {
                reservation.ReservationID,
                reservation.CustomerID,
                customerName = reservation.Customer?.Name,
                customerPhone = reservation.Customer?.Phone,
                reservation.TableID,
                tableName = reservation.RestaurantTable?.TableName,
                reservation.ReservationTime,
                reservation.NumberOfGuests,
                status = (int)reservation.Status,
                statusText = reservation.Status.ToString()
            };
        }
    }
}
