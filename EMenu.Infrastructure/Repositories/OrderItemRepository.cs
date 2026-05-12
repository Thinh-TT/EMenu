using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly AppDbContext _context;

        public OrderItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public OrderProduct? GetById(int orderProductId)
        {
            return _context.OrderProducts.FirstOrDefault(x => x.OrderProductID == orderProductId);
        }

        public IReadOnlyList<OrderProduct> GetBillableByOrderId(int orderId)
        {
            return _context.OrderProducts
                .Where(x => x.OrderID == orderId && x.Status != OrderItemStatus.Cancelled)
                .ToList();
        }

        public IReadOnlyList<KitchenPendingItemDto> GetPendingKitchenItems()
        {
            return _context.OrderProducts
                .Include(x => x.Product)
                .Include(x => x.Order)
                .Where(x => x.Status == OrderItemStatus.Pending || x.Status == OrderItemStatus.Preparing)
                .Select(x => new KitchenPendingItemDto
                {
                    OrderProductId = x.OrderProductID,
                    ProductName = x.Product.ProductName,
                    Quantity = x.Quantity,
                    Status = x.Status,
                    OrderId = x.Order.OrderID
                })
                .ToList();
        }

        public IReadOnlyList<DashboardTopProductDto> GetTopProducts(int count)
        {
            return _context.OrderProducts
                .Include(x => x.Product)
                .GroupBy(x => x.Product.ProductName)
                .Select(group => new DashboardTopProductDto
                {
                    Product = group.Key,
                    Quantity = group.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(count)
                .ToList();
        }

        public IReadOnlyList<MenuRecommendedProductDto> GetTopProductsByCategory(int countPerCategory)
        {
            if (countPerCategory <= 0)
            {
                return [];
            }

            var paidOrderIds = _context.Invoices
                .Select(x => x.OrderID);

            var rankedProducts = _context.OrderProducts
                .AsNoTracking()
                .Where(x =>
                    x.Status != OrderItemStatus.Cancelled &&
                    x.Product.IsAvailable &&
                    paidOrderIds.Contains(x.OrderID))
                .GroupBy(x => new
                {
                    x.Product.CategoryID,
                    x.ProductID,
                    x.Product.ProductName,
                    x.Product.Image,
                    x.Price,
                    x.Product.ProductType
                })
                .Select(group => new MenuRecommendedProductDto
                {
                    CategoryId = group.Key.CategoryID,
                    ProductId = group.Key.ProductID,
                    ProductName = group.Key.ProductName,
                    Image = group.Key.Image,
                    Price = group.Key.Price,
                    ProductType = group.Key.ProductType,
                    QuantitySold = group.Sum(x => x.Quantity)
                })
                .OrderBy(x => x.CategoryId)
                .ThenByDescending(x => x.QuantitySold)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.ProductId)
                .ToList();

            return rankedProducts
                .GroupBy(x => x.CategoryId)
                .SelectMany(group => group.Take(countPerCategory))
                .ToList();
        }
    }
}
