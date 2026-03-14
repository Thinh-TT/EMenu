using EMenu.Application.DTOs;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                throw new Exception("Session is not active");

            var existingOrder = _context.Orders
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

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
                throw new Exception("Quantity must be greater than 0");

            var session = _context.OrderSessions
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (session == null || session.Status != 1)
                throw new Exception("Session is not active");

            var order = _context.Orders
                .FirstOrDefault(o => o.OrderSessionID == sessionId);

            var product = _context.Products
                .FirstOrDefault(x => x.ProductID == productId);

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

            if (product == null)
                throw new Exception("Product not found");

            if (!product.IsAvailable)
                throw new Exception("Product is not available");

            var orderProduct = new OrderProduct
            {
                OrderID = order.OrderID,
                ProductID = productId,
                Quantity = quantity,
                Price = product.Price,
                Status = OrderItemStatus.Pending
            };

            _context.OrderProducts.Add(orderProduct);

            order.TotalAmount += product.Price * quantity;

            _context.SaveChanges();
        }

        public BillDto GetSessionBill(int sessionId)
        {
            var session = _context.OrderSessions
                .Include(s => s.RestaurantTable)
                .FirstOrDefault(s => s.OrderSessionID == sessionId);

            var orders = _context.Orders
                .Where(o => o.OrderSessionID == sessionId)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .ToList();

            var items = orders
                .SelectMany(o => o.OrderProducts)
                .Select(p => new BillItemDto
                {
                    ProductName = p.Product.ProductName,
                    Quantity = p.Quantity,
                    UnitPrice = p.Price
                }).ToList();

            return new BillDto
            {
                TableName = session.RestaurantTable.TableName,
                Items = items,
                TotalAmount = items.Sum(x => x.UnitPrice * x.Quantity)
            };
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
                throw new Exception("No staff account configured");

            return firstStaff.StaffID;
        }

    }
}
