using EMenu.Application.DTOs;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Enums;

namespace EMenu.Application.Services
{
    public class BillService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IOrderRepository _orderRepository;

        public BillService(
            ISessionRepository sessionRepository,
            IOrderRepository orderRepository)
        {
            _sessionRepository = sessionRepository;
            _orderRepository = orderRepository;
        }

        public int GetOrderIdBySession(int sessionId)
        {
            var order = _orderRepository.GetLatestBySession(sessionId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            return order.OrderID;
        }

        public BillDto GetBillBySessionId(int sessionId)
        {
            var session = _sessionRepository.GetByIdWithTable(sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            var orders = _orderRepository.GetBySessionWithDetails(sessionId);

            if (!orders.Any())
                throw new InvalidOperationException("Order not found.");

            var items = orders
                .SelectMany(x => x.OrderProducts)
                .Where(x => x.Status != OrderItemStatus.Cancelled)
                .Select(x => new BillItemDto
                {
                    ProductName = x.Product.ProductName,
                    Quantity = x.Quantity,
                    UnitPrice = x.Price
                })
                .ToList();

            if (!items.Any())
                throw new InvalidOperationException("Order has no billable items.");

            return new BillDto
            {
                OrderId = orders.OrderByDescending(x => x.OrderID).First().OrderID,
                TableName = session.RestaurantTable?.TableName,
                Items = items,
                TotalAmount = items.Sum(x => x.Total)
            };
        }

        public BillDto GetBillByOrderId(int orderId)
        {
            var order = _orderRepository.GetByIdWithDetails(orderId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            var items = order.OrderProducts
                .Where(x => x.Status != OrderItemStatus.Cancelled)
                .Select(op => new BillItemDto
                {
                    ProductName = op.Product.ProductName,
                    Quantity = op.Quantity,
                    UnitPrice = op.Price
                })
                .ToList();

            if (!items.Any())
                throw new InvalidOperationException("Order has no billable items.");

            return new BillDto
            {
                OrderId = orderId,
                TableName = order.OrderSession?.RestaurantTable?.TableName,
                Items = items,
                TotalAmount = items.Sum(i => i.Total)
            };
        }
    }
}
