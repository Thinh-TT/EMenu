using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<Product> GetAll()
        {
            return _context.Products.ToList();
        }

        public IReadOnlyList<Product> GetAllWithCategory()
        {
            return _context.Products
                .Include(x => x.Category)
                .ToList();
        }

        public IReadOnlyList<Product> GetByType(ProductType productType)
        {
            return _context.Products
                .Where(x => x.ProductType == productType)
                .ToList();
        }

        public IReadOnlyList<Product> GetFiltered(ProductFilterDto filter)
        {
            var query = _context.Products
                .Include(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(x => x.ProductName.Contains(filter.Name));

            if (filter.CategoryId.HasValue)
                query = query.Where(x => x.CategoryID == filter.CategoryId.Value);

            if (filter.MinPrice.HasValue)
                query = query.Where(x => x.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(x => x.Price <= filter.MaxPrice.Value);

            if (filter.ProductType.HasValue)
            {
                var type = (ProductType)filter.ProductType.Value;
                query = query.Where(x => x.ProductType == type);
            }

            return query.OrderBy(x => x.ProductID).ToList();
        }

        public Product? GetById(int productId)
        {
            return _context.Products.FirstOrDefault(x => x.ProductID == productId);
        }

        public void Add(Product product)
        {
            _context.Products.Add(product);
        }

        public void Update(Product product)
        {
            _context.Products.Update(product);
        }

        public void Remove(Product product)
        {
            _context.Products.Remove(product);
        }
    }
}
