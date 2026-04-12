using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using Moq;

namespace EMenu.Tests.Services;

public class ReservationServiceTests
{
    [Fact]
    public void CreateReservation_ValidInput_CreatesPendingReservation()
    {
        var reservationRepository = new Mock<IReservationRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        Reservation? added = null;

        customerRepository.Setup(x => x.Exists(5)).Returns(true);
        tableRepository.Setup(x => x.GetById(3))
            .Returns(new RestaurantTable { TableID = 3, TableName = "T03", Capacity = 4 });
        reservationRepository.Setup(x => x.HasConflict(3, It.IsAny<DateTime>(), null))
            .Returns(false);
        reservationRepository.Setup(x => x.Add(It.IsAny<Reservation>()))
            .Callback<Reservation>(x => added = x);

        var service = new ReservationService(
            reservationRepository.Object,
            customerRepository.Object,
            tableRepository.Object,
            unitOfWork.Object);

        var reservationTime = DateTime.Now.AddHours(3);
        var result = service.CreateReservation(5, 3, reservationTime, 3);

        Assert.NotNull(result);
        Assert.NotNull(added);
        Assert.Equal(5, added!.CustomerID);
        Assert.Equal(3, added.TableID);
        Assert.Equal(3, added.NumberOfGuests);
        Assert.Equal(ReservationStatus.Pending, added.Status);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Fact]
    public void CreateReservationForCustomer_WhenTimeConflict_Throws()
    {
        var reservationRepository = new Mock<IReservationRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        tableRepository.Setup(x => x.GetById(1))
            .Returns(new RestaurantTable { TableID = 1, TableName = "T01", Capacity = 2 });
        reservationRepository.Setup(x => x.HasConflict(1, It.IsAny<DateTime>(), null))
            .Returns(true);

        var service = new ReservationService(
            reservationRepository.Object,
            customerRepository.Object,
            tableRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.CreateReservationForCustomer(
                customerName: "Guest A",
                phone: "0900000000",
                email: "guest@example.com",
                tableId: 1,
                reservationTime: DateTime.Now.AddHours(2),
                numberOfGuests: 2));

        Assert.Equal("This table already has a reservation at the selected time.", ex.Message);
        customerRepository.Verify(x => x.Add(It.IsAny<Customer>()), Times.Never);
        unitOfWork.Verify(x => x.BeginTransaction(), Times.Never);
    }

    [Fact]
    public void ConfirmReservation_PendingReservation_UpdatesStatusToConfirmed()
    {
        var reservationRepository = new Mock<IReservationRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var reservation = new Reservation
        {
            ReservationID = 10,
            CustomerID = 5,
            TableID = 3,
            ReservationTime = DateTime.Now.AddHours(5),
            NumberOfGuests = 2,
            Status = ReservationStatus.Pending
        };

        reservationRepository.Setup(x => x.GetByIdWithDetails(10)).Returns(reservation);
        reservationRepository.Setup(x => x.HasConflict(3, reservation.ReservationTime, 10)).Returns(false);

        var service = new ReservationService(
            reservationRepository.Object,
            customerRepository.Object,
            tableRepository.Object,
            unitOfWork.Object);

        var result = service.ConfirmReservation(10);

        Assert.Equal(ReservationStatus.Confirmed, result.Status);
        reservationRepository.Verify(x => x.Update(reservation), Times.Once);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
    }
}
