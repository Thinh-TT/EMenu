using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.DTOs;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using Moq;

namespace EMenu.Tests.Services;

public class OrderServiceTests
{
    [Fact]
    public void AddProduct_ValidInput_AddsOrderItem()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var productRepository = new Mock<IProductRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        var order = new Order
        {
            OrderID = 10,
            Status = OrderStatus.Pending,
            TotalAmount = 0
        };
        OrderProduct? addedItem = null;

        sessionRepository.Setup(x => x.GetById(1)).Returns(new OrderSession { OrderSessionID = 1, Status = 1 });
        productRepository.Setup(x => x.GetById(2))
            .Returns(new Product { ProductID = 2, IsAvailable = true, Price = 50 });
        orderRepository.Setup(x => x.GetEditableBySession(1)).Returns(order);
        orderRepository.Setup(x => x.HasInvoice(10)).Returns(false);
        orderRepository.Setup(x => x.AddOrderProduct(It.IsAny<OrderProduct>()))
            .Callback<OrderProduct>(item => addedItem = item);
        unitOfWork.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        var service = new OrderService(
            sessionRepository.Object,
            orderRepository.Object,
            productRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        service.AddProduct(1, 2, 2);

        Assert.NotNull(addedItem);
        Assert.Equal(2, addedItem!.Quantity);
        Assert.Equal(2, addedItem.ProductID);
        Assert.Equal(OrderItemStatus.Pending, addedItem.Status);
        Assert.Equal(100, order.TotalAmount);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
        transaction.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public void AddProduct_WhenSessionInactive_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var productRepository = new Mock<IProductRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        sessionRepository.Setup(x => x.GetById(1)).Returns(new OrderSession { OrderSessionID = 1, Status = 0 });

        var service = new OrderService(
            sessionRepository.Object,
            orderRepository.Object,
            productRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.AddProduct(1, 2, 1));
        Assert.Equal("Session is not active.", ex.Message);
    }

    [Fact]
    public void AddProduct_WhenQuantityInvalid_Throws()
    {
        var service = new OrderService(
            Mock.Of<ISessionRepository>(),
            Mock.Of<IOrderRepository>(),
            Mock.Of<IProductRepository>(),
            Mock.Of<IStaffRepository>(),
            Mock.Of<IUnitOfWork>());

        var ex = Assert.Throws<ArgumentException>(() => service.AddProduct(1, 2, 0));
        Assert.Equal("Quantity must be greater than 0.", ex.Message);
    }

    [Fact]
    public void AddProduct_WhenProductUnavailable_Throws()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var productRepository = new Mock<IProductRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        sessionRepository.Setup(x => x.GetById(1)).Returns(new OrderSession { OrderSessionID = 1, Status = 1 });
        productRepository.Setup(x => x.GetById(2))
            .Returns(new Product { ProductID = 2, IsAvailable = false, Price = 10 });

        var service = new OrderService(
            sessionRepository.Object,
            orderRepository.Object,
            productRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.AddProduct(1, 2, 1));
        Assert.Equal("Product is not available.", ex.Message);
    }

    [Fact]
    public void SubmitOrder_WhenAnItemIsInvalid_DoesNotCommit()
    {
        var sessionRepository = new Mock<ISessionRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var productRepository = new Mock<IProductRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        var order = new Order
        {
            OrderID = 10,
            Status = OrderStatus.Pending,
            TotalAmount = 0
        };

        sessionRepository.Setup(x => x.GetById(1)).Returns(new OrderSession { OrderSessionID = 1, Status = 1 });
        orderRepository.Setup(x => x.GetEditableBySession(1)).Returns(order);
        orderRepository.Setup(x => x.HasInvoice(10)).Returns(false);
        unitOfWork.Setup(x => x.BeginTransaction()).Returns(transaction.Object);

        productRepository.Setup(x => x.GetById(100))
            .Returns(new Product { ProductID = 100, IsAvailable = true, Price = 20 });
        productRepository.Setup(x => x.GetById(200))
            .Returns(new Product { ProductID = 200, IsAvailable = false, Price = 30 });

        var service = new OrderService(
            sessionRepository.Object,
            orderRepository.Object,
            productRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() => service.SubmitOrder(
            1,
            new[]
            {
                new CartItemDto { ProductId = 100, Quantity = 1 },
                new CartItemDto { ProductId = 200, Quantity = 1 }
            }));

        Assert.Equal("Product is not available.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
        transaction.Verify(x => x.Commit(), Times.Never);
    }
}
