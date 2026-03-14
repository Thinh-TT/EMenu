using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Application.Services
{
    public class KitchenService
    {
        private readonly AppDbContext _context;

        public KitchenService(AppDbContext context)
        {
            _context = context;
        }

        public List<object> GetPendingItems()
        {
            var items = _context.OrderProducts
                .Include(x => x.Product)
                .Include(x => x.Order)
                .Where(x => x.Status == OrderItemStatus.Pending
                         || x.Status == OrderItemStatus.Preparing)
                .Select(x => new
                {
                    x.OrderProductID,
                    ProductName = x.Product.ProductName,
                    x.Quantity,
                    x.Status,
                    x.Order.OrderID
                })
                .ToList<object>();

            return items;
        }

        public void UpdateStatus(int orderProductId, OrderItemStatus status)
        {
            var item = _context.OrderProducts.Find(orderProductId);

            if (item == null)
                throw new InvalidOperationException("Order item not found.");

            if (!IsValidStatusTransition(item.Status, status))
                throw new InvalidOperationException("Invalid kitchen status transition.");

            item.Status = status;

            _context.SaveChanges();
        }

        private static bool IsValidStatusTransition(OrderItemStatus currentStatus, OrderItemStatus nextStatus)
        {
            if (currentStatus == nextStatus)
                return true;

            if (currentStatus == OrderItemStatus.Pending && nextStatus == OrderItemStatus.Preparing)
                return true;

            if (currentStatus == OrderItemStatus.Preparing && nextStatus == OrderItemStatus.Ready)
                return true;

            if (currentStatus == OrderItemStatus.Ready && nextStatus == OrderItemStatus.Served)
                return true;

            if (nextStatus == OrderItemStatus.Cancelled &&
                currentStatus != OrderItemStatus.Served &&
                currentStatus != OrderItemStatus.Cancelled)
                return true;

            return false;
        }
    }
}
