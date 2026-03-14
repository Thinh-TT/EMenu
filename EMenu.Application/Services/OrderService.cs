using EMenu.Application.DTOs;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Application.Services
{
    public class OrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public Order CreateOrder(int sessionId, int staffId)
        {
            var session = _context.OrderSessions
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (session == null || session.Status != 1)
                throw new InvalidOperationException("Session is not active.");

            var existingOrder = GetEditableOrder(sessionId);

            if (existingOrder != null)
                return existingOrder;

            var order = new Order
            {
                OrderSessionID = sessionId,
                StaffID = ResolveStaffId(staffId),
                CreatedTime = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = 0
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            return order;
        }

        public void AddProduct(int sessionId, int productId, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0.");

            var session = _context.OrderSessions
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (session == null || session.Status != 1)
                throw new InvalidOperationException("Session is not active.");

            var product = _context.Products
                .FirstOrDefault(x => x.ProductID == productId);

            if (product == null)
                throw new InvalidOperationException("Product not found.");

            if (!product.IsAvailable)
                throw new InvalidOperationException("Product is not available.");

            using var transaction = _context.Database.BeginTransaction();

            var order = GetEditableOrder(sessionId);

            if (order == null)
            {
                order = new Order
                {
                    OrderSessionID = sessionId,
                    StaffID = ResolveStaffId(),
                    CreatedTime = DateTime.Now,
                    Status = OrderStatus.Pending,
                    TotalAmount = 0
                };

                _context.Orders.Add(order);
                _context.SaveChanges();
            }

            if (_context.Invoices.Any(x => x.OrderID == order.OrderID))
                throw new InvalidOperationException("Order is already paid.");

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Order can no longer be modified.");

            var orderProduct = new OrderProduct
            {
                OrderID = order.OrderID,
                ProductID = productId,
                Quantity = quantity,
                Price = product.Price,
                Status = OrderItemStatus.Pending
            };

            _context.OrderProducts.Add(orderProduct);
            _context.SaveChanges();

            RecalculateOrderTotal(order.OrderID);

            transaction.Commit();
        }

        public void SubmitOrder(int sessionId, IEnumerable<CartItemDto> items)
        {
            var cartItems = items?.ToList() ?? new List<CartItemDto>();

            if (!cartItems.Any())
                throw new InvalidOperationException("Cart is empty.");

            var session = _context.OrderSessions
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (session == null || session.Status != 1)
                throw new InvalidOperationException("Session is not active.");

            using var transaction = _context.Database.BeginTransaction();

            var order = GetEditableOrder(sessionId);

            if (order == null)
            {
                order = new Order
                {
                    OrderSessionID = sessionId,
                    StaffID = ResolveStaffId(),
                    CreatedTime = DateTime.Now,
                    Status = OrderStatus.Pending,
                    TotalAmount = 0
                };

                _context.Orders.Add(order);
                _context.SaveChanges();
            }

            if (_context.Invoices.Any(x => x.OrderID == order.OrderID))
                throw new InvalidOperationException("Order is already paid.");

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Order can no longer be modified.");

            foreach (var item in cartItems)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException("Quantity must be greater than 0.");

                var product = _context.Products
                    .FirstOrDefault(x => x.ProductID == item.ProductId);

                if (product == null)
                    throw new InvalidOperationException("Product not found.");

                if (!product.IsAvailable)
                    throw new InvalidOperationException("Product is not available.");

                _context.OrderProducts.Add(new OrderProduct
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price,
                    Status = OrderItemStatus.Pending
                });
            }

            _context.SaveChanges();

            RecalculateOrderTotal(order.OrderID);

            transaction.Commit();
        }

        public BillDto GetSessionBill(int sessionId)
        {
            var session = _context.OrderSessions
                .Include(s => s.RestaurantTable)
                .FirstOrDefault(s => s.OrderSessionID == sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            var orders = _context.Orders
                .Where(o => o.OrderSessionID == sessionId)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .ToList();

            if (!orders.Any())
                throw new InvalidOperationException("Order not found.");

            var items = orders
                .SelectMany(o => o.OrderProducts)
                .Where(x => x.Status != OrderItemStatus.Cancelled)
                .Select(p => new BillItemDto
                {
                    ProductName = p.Product.ProductName,
                    Quantity = p.Quantity,
                    UnitPrice = p.Price
                })
                .ToList();

            if (!items.Any())
                throw new InvalidOperationException("Order has no billable items.");

            return new BillDto
            {
                TableName = session.RestaurantTable?.TableName,
                Items = items,
                TotalAmount = items.Sum(x => x.UnitPrice * x.Quantity)
            };
        }

        private Order? GetEditableOrder(int sessionId)
        {
            return _context.Orders
                .OrderByDescending(x => x.OrderID)
                .FirstOrDefault(x =>
                    x.OrderSessionID == sessionId &&
                    x.Status != OrderStatus.Completed &&
                    x.Status != OrderStatus.Cancelled &&
                    !_context.Invoices.Any(i => i.OrderID == x.OrderID));
        }

        private void RecalculateOrderTotal(int orderId)
        {
            var order = _context.Orders
                .Include(x => x.OrderProducts)
                .FirstOrDefault(x => x.OrderID == orderId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            order.TotalAmount = order.OrderProducts
                .Where(x => x.Status != OrderItemStatus.Cancelled)
                .Sum(x => x.Price * x.Quantity);

            _context.SaveChanges();
        }

        private int ResolveStaffId(int? staffId = null)
        {
            if (staffId.HasValue && staffId.Value > 0)
                return staffId.Value;

            var systemStaff = _context.Staffs
                .FirstOrDefault(x => x.StaffName == "System");

            if (systemStaff != null)
                return systemStaff.StaffID;

            var firstStaff = _context.Staffs
                .OrderBy(x => x.StaffID)
                .FirstOrDefault();

            if (firstStaff == null)
                throw new InvalidOperationException("No staff account configured.");

            return firstStaff.StaffID;
        }
    }
}
