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

    [Fact]
    public void TransferTable_ValidInput_MovesOrdersAndClosesSourceSession()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        var sourceTable = new RestaurantTable { TableID = 1, Status = 1 };
        var targetTable = new RestaurantTable { TableID = 2, Status = 0 };
        var sourceSession = new OrderSession
        {
            OrderSessionID = 11,
            TableID = 1,
            CustomerID = 100,
            Status = 1
        };

        sessionRepository.Setup(x => x.GetActiveByTable(1)).Returns(sourceSession);
        sessionRepository.Setup(x => x.HasActiveByTable(2)).Returns(false);
        sessionRepository.Setup(x => x.Add(It.IsAny<OrderSession>()))
            .Callback<OrderSession>(session => session.OrderSessionID = 12);

        tableRepository.Setup(x => x.GetById(1)).Returns(sourceTable);
        tableRepository.Setup(x => x.GetById(2)).Returns(targetTable);

        orderRepository.Setup(x => x.HasInvoicedOrder(11)).Returns(false);
        orderRepository.Setup(x => x.ReassignSession(11, 12)).Returns(3);

        unitOfWork.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var result = service.TransferTable(1, 2);

        Assert.Equal("Transfer", result.Operation);
        Assert.Equal(1, result.SourceTableId);
        Assert.Equal(2, result.TargetTableId);
        Assert.Equal(11, result.SourceSessionId);
        Assert.Equal(12, result.TargetSessionId);
        Assert.Equal(3, result.MovedOrderCount);
        Assert.Equal(0, sourceTable.Status);
        Assert.Equal(1, targetTable.Status);
        Assert.Equal(0, sourceSession.Status);
        Assert.NotNull(sourceSession.EndTime);

        unitOfWork.Verify(x => x.SaveChanges(), Times.Exactly(2));
        transaction.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public void TransferTable_WhenTargetNotAvailable_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        tableRepository.Setup(x => x.GetById(1))
            .Returns(new RestaurantTable { TableID = 1, Status = 1 });
        tableRepository.Setup(x => x.GetById(2))
            .Returns(new RestaurantTable { TableID = 2, Status = 1 });

        sessionRepository.Setup(x => x.GetActiveByTable(1))
            .Returns(new OrderSession { OrderSessionID = 11, TableID = 1, Status = 1 });

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.TransferTable(1, 2));

        Assert.Equal("Transfer target must be an available table.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Fact]
    public void TransferTable_WhenSourceHasInvoicedOrder_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        tableRepository.Setup(x => x.GetById(1))
            .Returns(new RestaurantTable { TableID = 1, Status = 1 });
        tableRepository.Setup(x => x.GetById(2))
            .Returns(new RestaurantTable { TableID = 2, Status = 0 });

        sessionRepository.Setup(x => x.GetActiveByTable(1))
            .Returns(new OrderSession { OrderSessionID = 11, TableID = 1, Status = 1 });
        sessionRepository.Setup(x => x.HasActiveByTable(2)).Returns(false);

        orderRepository.Setup(x => x.HasInvoicedOrder(11)).Returns(true);

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.TransferTable(1, 2));

        Assert.Equal("Cannot transfer or merge sessions with invoiced orders.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
    }

    [Fact]
    public void MergeTable_TargetOccupied_MovesOrdersIntoTargetSession()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        var sourceTable = new RestaurantTable { TableID = 1, Status = 1 };
        var targetTable = new RestaurantTable { TableID = 2, Status = 1 };
        var sourceSession = new OrderSession
        {
            OrderSessionID = 11,
            TableID = 1,
            CustomerID = 100,
            Status = 1
        };
        var targetSession = new OrderSession
        {
            OrderSessionID = 22,
            TableID = 2,
            CustomerID = 200,
            Status = 1
        };

        tableRepository.Setup(x => x.GetById(1)).Returns(sourceTable);
        tableRepository.Setup(x => x.GetById(2)).Returns(targetTable);
        sessionRepository.Setup(x => x.GetActiveByTable(1)).Returns(sourceSession);
        sessionRepository.Setup(x => x.GetActiveByTable(2)).Returns(targetSession);

        orderRepository.Setup(x => x.HasInvoicedOrder(11)).Returns(false);
        orderRepository.Setup(x => x.HasInvoicedOrder(22)).Returns(false);
        orderRepository.Setup(x => x.ReassignSession(11, 22)).Returns(4);

        unitOfWork.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var result = service.MergeTable(1, 2);

        Assert.Equal("Merge", result.Operation);
        Assert.Equal(11, result.SourceSessionId);
        Assert.Equal(22, result.TargetSessionId);
        Assert.Equal(4, result.MovedOrderCount);
        Assert.Equal(0, sourceTable.Status);
        Assert.Equal(1, targetTable.Status);
        Assert.Equal(200, targetSession.CustomerID);
        Assert.Equal(0, sourceSession.Status);
        Assert.NotNull(sourceSession.EndTime);

        sessionRepository.Verify(x => x.Add(It.IsAny<OrderSession>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
        transaction.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public void MergeTable_TargetAvailable_CreatesTargetSessionThenMovesOrders()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        var sourceTable = new RestaurantTable { TableID = 1, Status = 1 };
        var targetTable = new RestaurantTable { TableID = 2, Status = 0 };
        var sourceSession = new OrderSession
        {
            OrderSessionID = 11,
            TableID = 1,
            CustomerID = 100,
            Status = 1
        };

        tableRepository.Setup(x => x.GetById(1)).Returns(sourceTable);
        tableRepository.Setup(x => x.GetById(2)).Returns(targetTable);
        sessionRepository.Setup(x => x.GetActiveByTable(1)).Returns(sourceSession);
        sessionRepository.Setup(x => x.HasActiveByTable(2)).Returns(false);
        sessionRepository.Setup(x => x.Add(It.IsAny<OrderSession>()))
            .Callback<OrderSession>(session => session.OrderSessionID = 33);

        orderRepository.Setup(x => x.HasInvoicedOrder(11)).Returns(false);
        orderRepository.Setup(x => x.ReassignSession(11, 33)).Returns(2);

        unitOfWork.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var result = service.MergeTable(1, 2);

        Assert.Equal(33, result.TargetSessionId);
        Assert.Equal(2, result.MovedOrderCount);
        Assert.Equal(1, targetTable.Status);
        Assert.Equal(0, sourceTable.Status);

        sessionRepository.Verify(x => x.Add(It.IsAny<OrderSession>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Exactly(2));
        transaction.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public void MergeTable_WhenTargetReserved_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var customerRepository = new Mock<ICustomerRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        tableRepository.Setup(x => x.GetById(1))
            .Returns(new RestaurantTable { TableID = 1, Status = 1 });
        tableRepository.Setup(x => x.GetById(2))
            .Returns(new RestaurantTable { TableID = 2, Status = 2 });
        sessionRepository.Setup(x => x.GetActiveByTable(1))
            .Returns(new OrderSession { OrderSessionID = 11, TableID = 1, Status = 1 });

        var service = new SessionService(
            sessionRepository.Object,
            tableRepository.Object,
            customerRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.MergeTable(1, 2));

        Assert.Equal("Cannot merge into a reserved table.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
    }
}
