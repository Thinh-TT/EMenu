using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using Moq;

namespace EMenu.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public void GetExtendedSummary_ReturnsExtendedKpis()
    {
        var paymentRepository = new Mock<IPaymentRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var ingredientRepository = new Mock<IIngredientRepository>();
        var receiptRepository = new Mock<IReceiptRepository>();
        var reservationRepository = new Mock<IReservationRepository>();
        var timekeepingRepository = new Mock<ITimekeepingRepository>();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        paymentRepository.Setup(x => x.GetRevenueByDate(today)).Returns(1_500_000m);
        orderRepository.Setup(x => x.CountByCreatedDate(today)).Returns(12);
        tableRepository.Setup(x => x.CountInUse()).Returns(6);

        ingredientRepository.Setup(x => x.GetLowStock())
            .Returns(new List<Ingredient>
            {
                new() { IngredientID = 1, Name = "Rice", Unit = "kg", StockQuantity = 3m, MinStock = 5m },
                new() { IngredientID = 2, Name = "Fish Sauce", Unit = "L", StockQuantity = 1m, MinStock = 2m }
            });

        var receiptToday = new Receipt
        {
            ReceiptIngredients =
            [
                new ReceiptIngredient { Quantity = 2m, Price = 100m }
            ]
        };
        var receiptMonthExtra = new Receipt
        {
            ReceiptIngredients =
            [
                new ReceiptIngredient { Quantity = 3m, Price = 200m }
            ]
        };

        receiptRepository
            .Setup(x => x.GetByFilter(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null))
            .Returns<DateTime?, DateTime?, int?>((fromDate, toDate, _) =>
            {
                if (fromDate?.Date == today && toDate?.Date == today)
                {
                    return [receiptToday];
                }

                if (fromDate?.Date == monthStart && toDate?.Date == monthEnd)
                {
                    return [receiptToday, receiptMonthExtra];
                }

                return [];
            });

        reservationRepository
            .Setup(x => x.GetByFilter(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, null))
            .Returns(new List<Reservation>
            {
                new() { ReservationID = 1, Status = ReservationStatus.Pending },
                new() { ReservationID = 2, Status = ReservationStatus.Confirmed },
                new() { ReservationID = 3, Status = ReservationStatus.Cancelled }
            });

        timekeepingRepository
            .Setup(x => x.GetByMonth(today.Year, today.Month))
            .Returns(new List<Timekeeping>
            {
                new()
                {
                    Id = 1,
                    StaffID = 10,
                    Date = DateOnly.FromDateTime(today),
                    CheckIn = today.AddHours(8),
                    CheckOut = today.AddHours(16)
                },
                new()
                {
                    Id = 2,
                    StaffID = 11,
                    Date = DateOnly.FromDateTime(today),
                    CheckIn = today.AddHours(9),
                    CheckOut = null
                }
            });

        var service = new DashboardService(
            paymentRepository.Object,
            orderItemRepository.Object,
            orderRepository.Object,
            tableRepository.Object,
            ingredientRepository.Object,
            receiptRepository.Object,
            reservationRepository.Object,
            timekeepingRepository.Object);

        var summary = service.GetExtendedSummary();

        Assert.Equal(1_500_000m, summary.TodayRevenue);
        Assert.Equal(12, summary.OrdersToday);
        Assert.Equal(6, summary.TablesInUse);
        Assert.Equal(2, summary.LowStockCount);
        Assert.Equal(200m, summary.ImportValueToday);
        Assert.Equal(800m, summary.ImportValueThisMonth);
        Assert.Equal(2, summary.ReservationsToday);
        Assert.Equal(8m, summary.StaffHoursToday);
    }

    [Fact]
    public void GetImportTrend_FillsMissingDaysWithZero()
    {
        var paymentRepository = new Mock<IPaymentRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var ingredientRepository = new Mock<IIngredientRepository>();
        var receiptRepository = new Mock<IReceiptRepository>();
        var reservationRepository = new Mock<IReservationRepository>();
        var timekeepingRepository = new Mock<ITimekeepingRepository>();

        var today = DateTime.Today;
        var twoDaysAgo = today.AddDays(-2);

        receiptRepository
            .Setup(x => x.GetByFilter(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null))
            .Returns(new List<Receipt>
            {
                new()
                {
                    CreatedDate = twoDaysAgo,
                    ReceiptIngredients =
                    [
                        new ReceiptIngredient { Quantity = 1m, Price = 300m }
                    ]
                },
                new()
                {
                    CreatedDate = today,
                    ReceiptIngredients =
                    [
                        new ReceiptIngredient { Quantity = 2m, Price = 250m }
                    ]
                }
            });

        var service = new DashboardService(
            paymentRepository.Object,
            orderItemRepository.Object,
            orderRepository.Object,
            tableRepository.Object,
            ingredientRepository.Object,
            receiptRepository.Object,
            reservationRepository.Object,
            timekeepingRepository.Object);

        var trend = service.GetImportTrend(days: 3);

        Assert.Equal(3, trend.Count);
        Assert.Equal(300m, trend[0].TotalValue);
        Assert.Equal(0m, trend[1].TotalValue);
        Assert.Equal(500m, trend[2].TotalValue);
    }

    [Fact]
    public void GetAlerts_ReturnsLowStockAndReservationClashes()
    {
        var paymentRepository = new Mock<IPaymentRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var ingredientRepository = new Mock<IIngredientRepository>();
        var receiptRepository = new Mock<IReceiptRepository>();
        var reservationRepository = new Mock<IReservationRepository>();
        var timekeepingRepository = new Mock<ITimekeepingRepository>();

        ingredientRepository.Setup(x => x.GetLowStock())
            .Returns(new List<Ingredient>
            {
                new() { IngredientID = 1, Name = "Rice", Unit = "kg", StockQuantity = 3m, MinStock = 5m }
            });

        var clashTime = DateTime.Today.AddHours(19);
        reservationRepository
            .Setup(x => x.GetByFilter(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, null))
            .Returns(new List<Reservation>
            {
                new()
                {
                    ReservationID = 1,
                    TableID = 2,
                    ReservationTime = clashTime,
                    Status = ReservationStatus.Pending,
                    RestaurantTable = new RestaurantTable { TableID = 2, TableName = "T02" }
                },
                new()
                {
                    ReservationID = 2,
                    TableID = 2,
                    ReservationTime = clashTime,
                    Status = ReservationStatus.Confirmed,
                    RestaurantTable = new RestaurantTable { TableID = 2, TableName = "T02" }
                },
                new()
                {
                    ReservationID = 3,
                    TableID = 2,
                    ReservationTime = clashTime,
                    Status = ReservationStatus.Cancelled,
                    RestaurantTable = new RestaurantTable { TableID = 2, TableName = "T02" }
                }
            });

        var service = new DashboardService(
            paymentRepository.Object,
            orderItemRepository.Object,
            orderRepository.Object,
            tableRepository.Object,
            ingredientRepository.Object,
            receiptRepository.Object,
            reservationRepository.Object,
            timekeepingRepository.Object);

        var alerts = service.GetAlerts();

        Assert.Single(alerts.LowStockWarnings);
        Assert.Single(alerts.ReservationClashes);
        Assert.Equal(2, alerts.ReservationClashes[0].ConflictCount);
        Assert.Equal("T02", alerts.ReservationClashes[0].TableName);
    }
}
