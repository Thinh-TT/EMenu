using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using Moq;

namespace EMenu.Tests.Services;

public class PaymentServiceTests
{
    [Fact]
    public void PayCash_ValidInput_CompletesPaymentAndClosesSession()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        var session = new OrderSession { OrderSessionID = 1, TableID = 5, Status = 1 };
        var order = new Order { OrderID = 10, OrderSessionID = 1, Status = OrderStatus.Pending };
        var table = new RestaurantTable { TableID = 5, Status = 1 };

        sessionRepository.Setup(x => x.GetById(1)).Returns(session);
        orderRepository.Setup(x => x.GetLatestBySession(1)).Returns(order);
        orderItemRepository.Setup(x => x.GetBillableByOrderId(10)).Returns(new List<OrderProduct>
        {
            new() { Quantity = 2, Price = 100, Status = OrderItemStatus.Pending }
        });
        paymentRepository.Setup(x => x.GetInvoiceByOrderId(10)).Returns((Invoice?)null);
        tableRepository.Setup(x => x.GetById(5)).Returns(table);
        unitOfWork.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        var service = new PaymentService(
            sessionRepository.Object,
            orderRepository.Object,
            orderItemRepository.Object,
            tableRepository.Object,
            paymentRepository.Object,
            unitOfWork.Object);

        service.PayCash(1);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(200, order.TotalAmount);
        Assert.Equal(0, session.Status);
        Assert.Equal(0, table.Status);
        Assert.NotNull(session.EndTime);
        paymentRepository.Verify(x => x.AddInvoice(It.IsAny<Invoice>()), Times.Once);
        paymentRepository.Verify(x => x.AddPayment(It.IsAny<Payment>()), Times.Once);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
        transaction.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public void PayCash_WhenAlreadyPaid_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        sessionRepository.Setup(x => x.GetById(1)).Returns(new OrderSession { OrderSessionID = 1, TableID = 5, Status = 1 });
        orderRepository.Setup(x => x.GetLatestBySession(1)).Returns(new Order { OrderID = 10, OrderSessionID = 1 });
        orderItemRepository.Setup(x => x.GetBillableByOrderId(10)).Returns(new List<OrderProduct>
        {
            new() { Quantity = 1, Price = 50, Status = OrderItemStatus.Pending }
        });
        paymentRepository.Setup(x => x.GetInvoiceByOrderId(10)).Returns(new Invoice { InvoiceID = 99, OrderID = 10 });

        var service = new PaymentService(
            sessionRepository.Object,
            orderRepository.Object,
            orderItemRepository.Object,
            tableRepository.Object,
            paymentRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.PayCash(1));

        Assert.Equal("Order is already paid.", ex.Message);
        unitOfWork.Verify(x => x.BeginTransaction(), Times.Never);
    }

    [Fact]
    public void PayCash_WhenOrderHasNoBillableItems_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var tableRepository = new Mock<ITableRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        sessionRepository.Setup(x => x.GetById(1)).Returns(new OrderSession { OrderSessionID = 1, TableID = 5, Status = 1 });
        orderRepository.Setup(x => x.GetLatestBySession(1)).Returns(new Order { OrderID = 10, OrderSessionID = 1 });
        orderItemRepository.Setup(x => x.GetBillableByOrderId(10)).Returns(new List<OrderProduct>());

        var service = new PaymentService(
            sessionRepository.Object,
            orderRepository.Object,
            orderItemRepository.Object,
            tableRepository.Object,
            paymentRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.PayCash(1));

        Assert.Equal("Order has no billable items.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
    }
}
