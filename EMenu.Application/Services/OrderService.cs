using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
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
            var order = new Order
            {
                OrderSessionID = sessionId,
                StaffID = staffId,
                CreatedTime = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = 0
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            return order;
        }

        public OrderProduct AddProduct(int orderId, int productId, int quantity)
        {
            var product = _context.Products.Find(productId);

            var orderProduct = new OrderProduct
            {
                OrderID = orderId,
                ProductID = productId,
                Quantity = quantity,
                Price = product.Price,
                Status = OrderItemStatus.Pending
            };

            _context.OrderProducts.Add(orderProduct);

            var order = _context.Orders.Find(orderId);
            order.TotalAmount += product.Price * quantity;

            _context.SaveChanges();

            return orderProduct;
        }
    }
}
