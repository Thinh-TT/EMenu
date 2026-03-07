using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                throw new Exception("Order item not found");

            item.Status = status;

            _context.SaveChanges();
        }
    }
}
