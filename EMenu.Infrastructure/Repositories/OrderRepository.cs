using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public Order? GetById(int orderId)
        {
            return _context.Orders.FirstOrDefault(x => x.OrderID == orderId);
        }

        public Order? GetByIdWithDetails(int orderId)
        {
            return _context.Orders
                .Include(x => x.OrderSession)
                .ThenInclude(x => x.RestaurantTable)
                .Include(x => x.OrderProducts)
                .ThenInclude(x => x.Product)
                .FirstOrDefault(x => x.OrderID == orderId);
        }

        public Order? GetLatestBySession(int sessionId)
        {
            return _context.Orders
                .OrderByDescending(x => x.OrderID)
                .FirstOrDefault(x => x.OrderSessionID == sessionId);
        }

        public Order? GetEditableBySession(int sessionId)
        {
            return _context.Orders
                .OrderByDescending(x => x.OrderID)
                .FirstOrDefault(x =>
                    x.OrderSessionID == sessionId &&
                    x.Status != OrderStatus.Completed &&
                    x.Status != OrderStatus.Cancelled &&
                    !_context.Invoices.Any(i => i.OrderID == x.OrderID));
        }

        public IReadOnlyList<Order> GetBySessionWithDetails(int sessionId)
        {
            return _context.Orders
                .Where(x => x.OrderSessionID == sessionId)
                .Include(x => x.OrderProducts)
                .ThenInclude(x => x.Product)
                .ToList();
        }

        public int CountByCreatedDate(DateTime date)
        {
            var targetDate = date.Date;

            return _context.Orders
                .Count(x => x.CreatedTime.Date == targetDate);
        }

        public bool HasInvoice(int orderId)
        {
            return _context.Invoices.Any(x => x.OrderID == orderId);
        }

        public bool HasUnpaidBillableOrder(int sessionId)
        {
            return _context.Orders
                .Where(x => x.OrderSessionID == sessionId)
                .Any(order =>
                    _context.OrderProducts.Any(item =>
                        item.OrderID == order.OrderID &&
                        item.Status != OrderItemStatus.Cancelled) &&
                    !_context.Invoices.Any(invoice => invoice.OrderID == order.OrderID));
        }

        public void Add(Order order)
        {
            _context.Orders.Add(order);
        }

        public void AddOrderProduct(OrderProduct orderProduct)
        {
            _context.OrderProducts.Add(orderProduct);
        }
    }
}
