using EMenu.Application.Abstractions.DTOs;
using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IOrderItemRepository
    {
        OrderProduct? GetById(int orderProductId);
        IReadOnlyList<OrderProduct> GetBillableByOrderId(int orderId);
        IReadOnlyList<KitchenPendingItemDto> GetPendingKitchenItems();
        IReadOnlyList<DashboardTopProductDto> GetTopProducts(int count);
    }
}
