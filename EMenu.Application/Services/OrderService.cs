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
                StaffID = 1,
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
            // tìm order theo session
            var order = _context.Orders
                .FirstOrDefault(o => o.OrderSessionID == sessionId);

            var product = _context.Products
        .FirstOrDefault(x => x.ProductID == productId);

            // nếu chưa có order → tạo mới
            if (order == null)
            {
                order = new Order
                {
                    OrderSessionID = sessionId,
                    StaffID = 1,
                    CreatedTime = DateTime.Now,
                    Status = (OrderStatus)1,
                    TotalAmount = 0
                };

                _context.Orders.Add(order);
                _context.SaveChanges();
            }
            if (product == null)
                throw new Exception("Product not found");


            // thêm sản phẩm vào order
            var orderProduct = new OrderProduct
            {
                OrderID = order.OrderID,
                ProductID = productId,
                Quantity = quantity,
                Price = product.Price,
                Status = (OrderItemStatus)1
            };

            _context.OrderProducts.Add(orderProduct);

            order.TotalAmount += product.Price * quantity;

            _context.SaveChanges();
        }

    }
}
