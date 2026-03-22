using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using Moq;

namespace EMenu.Tests.Services;

public class KitchenServiceTests
{
    [Fact]
    public void UpdateStatus_ValidTransition_UpdatesAndSaves()
    {
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var item = new OrderProduct
        {
            OrderProductID = 1,
            Status = OrderItemStatus.Pending
        };

        orderItemRepository.Setup(x => x.GetById(1)).Returns(item);

        var service = new KitchenService(orderItemRepository.Object, unitOfWork.Object);

        service.UpdateStatus(1, OrderItemStatus.Preparing);

        Assert.Equal(OrderItemStatus.Preparing, item.Status);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Fact]
    public void UpdateStatus_InvalidTransition_Throws()
    {
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var item = new OrderProduct
        {
            OrderProductID = 1,
            Status = OrderItemStatus.Ready
        };

        orderItemRepository.Setup(x => x.GetById(1)).Returns(item);

        var service = new KitchenService(orderItemRepository.Object, unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.UpdateStatus(1, OrderItemStatus.Preparing));

        Assert.Equal("Invalid kitchen status transition.", ex.Message);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Never);
    }
}
