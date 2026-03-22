using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using Moq;

namespace EMenu.Tests.Services;

public class SessionServiceTests
{
    [Fact]
    public void StartSession_ValidInput_StartsSessionAndMarksTableOccupied()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        var table = new RestaurantTable { TableID = 1, Status = 0 };
        OrderSession? addedSession = null;

        tableRepository.Setup(x => x.GetById(1)).Returns(table);
        customerRepository.Setup(x => x.Exists(10)).Returns(true);
        sessionRepository.Setup(x => x.HasActiveByTable(1)).Returns(false);
        sessionRepository.Setup(x => x.Add(It.IsAny<OrderSession>()))
            .Callback<OrderSession>(s => addedSession = s);
        unitOfWork.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var session = service.StartSession(1, 10);

        Assert.NotNull(session);
        Assert.Equal(1, table.Status);
        Assert.NotNull(addedSession);
        Assert.Equal(1, addedSession!.Status);
        Assert.Equal(1, addedSession.TableID);
        Assert.Equal(10, addedSession.CustomerID);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
        transaction.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public void StartSession_TableOccupied_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        tableRepository.Setup(x => x.GetById(1)).Returns(new RestaurantTable { TableID = 1, Status = 1 });
        customerRepository.Setup(x => x.Exists(10)).Returns(true);
        sessionRepository.Setup(x => x.HasActiveByTable(1)).Returns(false);

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.StartSession(1, 10));

        Assert.Equal("Table is already occupied.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Fact]
    public void EndSessionById_WithUnpaidOrder_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        sessionRepository.Setup(x => x.GetById(7)).Returns(new OrderSession { OrderSessionID = 7, TableID = 1, Status = 1 });
        orderRepository.Setup(x => x.HasUnpaidBillableOrder(7)).Returns(true);

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.EndSessionById(7));

        Assert.Equal("Cannot close session with unpaid order.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Fact]
    public void EndSessionById_ValidInput_ClosesSessionAndFreesTable()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        var session = new OrderSession { OrderSessionID = 7, TableID = 1, Status = 1 };
        var table = new RestaurantTable { TableID = 1, Status = 1 };

        sessionRepository.Setup(x => x.GetById(7)).Returns(session);
        orderRepository.Setup(x => x.HasUnpaidBillableOrder(7)).Returns(false);
        tableRepository.Setup(x => x.GetById(1)).Returns(table);
        unitOfWork.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        service.EndSessionById(7);

        Assert.Equal(0, session.Status);
        Assert.Equal(0, table.Status);
        Assert.NotNull(session.EndTime);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
        transaction.Verify(x => x.Commit(), Times.Once);
    }
}
