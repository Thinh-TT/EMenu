using EMenu.Application.DTOs;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Application.Services
{
    public class BillService
    {
        private readonly AppDbContext _context;

        public BillService(AppDbContext context)
        {
            _context = context;
        }

        // lấy orderId từ sessionId
        public int GetOrderIdBySession(int sessionId)
        {
            var order = _context.Orders
                .OrderByDescending(o => o.OrderID)
                .FirstOrDefault(o => o.OrderSessionID == sessionId);

            if (order == null)
                throw new Exception("Order not found");

            return order.OrderID;
        }

        public BillDto GetBillBySessionId(int sessionId)
        {
            var session = _context.OrderSessions
                .Include(x => x.RestaurantTable)
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (session == null)
                throw new Exception("Session not found");

            var orders = _context.Orders
                .Where(x => x.OrderSessionID == sessionId)
                .Include(x => x.OrderProducts)
                .ThenInclude(x => x.Product)
                .ToList();

            if (!orders.Any())
                throw new Exception("Order not found");

            var items = orders
                .SelectMany(x => x.OrderProducts)
                .Select(x => new BillItemDto
                {
                    ProductName = x.Product.ProductName,
                    Quantity = x.Quantity,
                    UnitPrice = x.Price
                })
                .ToList();

            return new BillDto
            {
                OrderId = orders.OrderByDescending(x => x.OrderID).First().OrderID,
                TableName = session.RestaurantTable?.TableName,
                Items = items,
                TotalAmount = items.Sum(x => x.Total)
            };
        }

        // tạo bill từ orderId
        public BillDto GetBillByOrderId(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderSession)
                .ThenInclude(x => x.RestaurantTable)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
                throw new Exception("Order not found");

            var items = order.OrderProducts
                .Select(op => new BillItemDto
                {
                    ProductName = op.Product.ProductName,
                    Quantity = op.Quantity,
                    UnitPrice = op.Price
                })
                .ToList();

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
