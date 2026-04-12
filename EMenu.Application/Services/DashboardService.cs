using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;

namespace EMenu.Application.Services
{
    public class DashboardService
    {
        private const int DefaultTopProductsCount = 5;
        private const int DefaultImportTrendDays = 7;
        private const int DefaultAlertItemLimit = 5;
        private const int DefaultClashLookaheadDays = 7;

        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly IReceiptRepository _receiptRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly ITimekeepingRepository _timekeepingRepository;

        public DashboardService(
            IPaymentRepository paymentRepository,
            IOrderItemRepository orderItemRepository,
            IOrderRepository orderRepository,
            ITableRepository tableRepository,
            IIngredientRepository ingredientRepository,
            IReceiptRepository receiptRepository,
            IReservationRepository reservationRepository,
            ITimekeepingRepository timekeepingRepository)
        {
            _paymentRepository = paymentRepository;
            _orderItemRepository = orderItemRepository;
            _orderRepository = orderRepository;
            _tableRepository = tableRepository;
            _ingredientRepository = ingredientRepository;
            _receiptRepository = receiptRepository;
            _reservationRepository = reservationRepository;
            _timekeepingRepository = timekeepingRepository;
        }

        public decimal GetTodayRevenue()
        {
            return _paymentRepository.GetRevenueByDate(DateTime.Today);
        }

        public IReadOnlyList<DashboardTopProductDto> GetTopProducts()
        {
            return _orderItemRepository.GetTopProducts(DefaultTopProductsCount);
        }

        public TableStatusSummaryDto GetTableStatus()
        {
            return new TableStatusSummaryDto
            {
                TotalTables = _tableRepository.Count(),
                OccupiedTables = _tableRepository.CountInUse()
            };
        }

        public int GetTodayOrderCount()
        {
            return _orderRepository.CountByCreatedDate(DateTime.Today);
        }

        public int GetTablesInUseCount()
        {
            return _tableRepository.CountInUse();
        }

        public DashboardExtendedSummaryDto GetExtendedSummary()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            return new DashboardExtendedSummaryDto
            {
                TodayRevenue = GetTodayRevenue(),
                OrdersToday = GetTodayOrderCount(),
                TablesInUse = GetTablesInUseCount(),
                LowStockCount = _ingredientRepository.GetLowStock().Count,
                ImportValueToday = GetImportValue(today, today),
                ImportValueThisMonth = GetImportValue(monthStart, monthEnd),
                ReservationsToday = GetReservationsCountByDate(today),
                StaffHoursToday = GetStaffWorkedHoursByDate(today)
            };
        }

        public IReadOnlyList<DashboardImportTrendPointDto> GetImportTrend(int days = DefaultImportTrendDays)
        {
            if (days < 1 || days > 31)
            {
                throw new InvalidOperationException("Days must be between 1 and 31.");
            }

            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-(days - 1));
            var receipts = _receiptRepository.GetByFilter(startDate, endDate, supplierId: null);
            var totalsByDate = receipts
                .GroupBy(x => x.CreatedDate.Date)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(CalculateReceiptTotal));

            var points = new List<DashboardImportTrendPointDto>(days);

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                totalsByDate.TryGetValue(date, out var totalValue);

                points.Add(new DashboardImportTrendPointDto
                {
                    Date = date,
                    Label = date.ToString("dd/MM"),
                    TotalValue = totalValue
                });
            }

            return points;
        }

        public DashboardAlertsDto GetAlerts(
            int lowStockLimit = DefaultAlertItemLimit,
            int clashLookaheadDays = DefaultClashLookaheadDays)
        {
            if (lowStockLimit < 1 || lowStockLimit > 50)
            {
                throw new InvalidOperationException("Low-stock limit must be between 1 and 50.");
            }

            if (clashLookaheadDays < 1 || clashLookaheadDays > 31)
            {
                throw new InvalidOperationException("Clash lookahead days must be between 1 and 31.");
            }

            var lowStockWarnings = _ingredientRepository.GetLowStock()
                .Take(lowStockLimit)
                .Select(x => new DashboardLowStockAlertDto
                {
                    IngredientId = x.IngredientID,
                    IngredientName = x.Name,
                    Unit = x.Unit,
                    StockQuantity = x.StockQuantity,
                    MinStock = x.MinStock
                })
                .ToList();

            var clashFromDate = DateTime.Today;
            var clashToDate = DateTime.Today.AddDays(clashLookaheadDays - 1).Date.AddDays(1).AddTicks(-1);
            var reservationClashes = _reservationRepository
                .GetByFilter(clashFromDate, clashToDate, tableId: null, status: null)
                .Where(x => x.Status != ReservationStatus.Cancelled)
                .GroupBy(x => new
                {
                    x.TableID,
                    x.ReservationTime,
                    TableName = x.RestaurantTable?.TableName ?? $"Table {x.TableID}"
                })
                .Where(group => group.Count() > 1)
                .OrderBy(group => group.Key.ReservationTime)
                .ThenBy(group => group.Key.TableID)
                .Select(group => new DashboardReservationClashAlertDto
                {
                    TableId = group.Key.TableID,
                    TableName = group.Key.TableName,
                    ReservationTime = group.Key.ReservationTime,
                    ConflictCount = group.Count()
                })
                .ToList();

            return new DashboardAlertsDto
            {
                LowStockWarnings = lowStockWarnings,
                ReservationClashes = reservationClashes
            };
        }

        private decimal GetImportValue(DateTime fromDate, DateTime toDate)
        {
            var receipts = _receiptRepository.GetByFilter(fromDate, toDate, supplierId: null);
            return receipts.Sum(CalculateReceiptTotal);
        }

        private int GetReservationsCountByDate(DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1).AddTicks(-1);

            return _reservationRepository
                .GetByFilter(dayStart, dayEnd, tableId: null, status: null)
                .Count(x => x.Status != ReservationStatus.Cancelled);
        }

        private decimal GetStaffWorkedHoursByDate(DateTime date)
        {
            var records = _timekeepingRepository.GetByMonth(date.Year, date.Month)
                .Where(x => x.Date == DateOnly.FromDateTime(date))
                .ToList();

            return Math.Round(records.Sum(CalculateWorkedHours), 2);
        }

        private static decimal CalculateReceiptTotal(Receipt receipt)
        {
            if (receipt.ReceiptIngredients == null || receipt.ReceiptIngredients.Count == 0)
            {
                return 0m;
            }

            return receipt.ReceiptIngredients.Sum(x => x.Quantity * x.Price);
        }

        private static decimal CalculateWorkedHours(Timekeeping record)
        {
            if (!record.CheckOut.HasValue || record.CheckOut.Value <= record.CheckIn)
            {
                return 0m;
            }

            return (decimal)(record.CheckOut.Value - record.CheckIn).TotalHours;
        }
    }
}
