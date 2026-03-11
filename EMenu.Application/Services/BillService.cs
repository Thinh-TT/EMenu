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
                .FirstOrDefault(o => o.OrderSessionID == sessionId);

            if (order == null)
                throw new Exception("Order not found");

            return order.OrderID;
        }

        // tạo bill từ orderId
        public BillDto GetBillByOrderId(int orderId)
        {
            var order = _context.Orders
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
                Items = items,
                TotalAmount = items.Sum(i => i.Total)
            };
        }
    }
}
