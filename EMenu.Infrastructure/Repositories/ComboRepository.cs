using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;

namespace EMenu.Infrastructure.Repositories
{
    public class ComboRepository : IComboRepository
    {
        private readonly AppDbContext _context;

        public ComboRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<int> GetComboItemIds(int comboId)
        {
            return _context.ComboProducts
                .Where(x => x.ProductID == comboId)
                .Select(x => x.ComboID)
                .ToList();
        }

        public IReadOnlyList<ComboItemDto> GetComboItems(int comboId)
        {
            return _context.ComboProducts
                .Where(x => x.ProductID == comboId)
                .Join(
                    _context.Products,
                    comboProduct => comboProduct.ComboID,
                    product => product.ProductID,
                    (comboProduct, product) => new ComboItemDto
                    {
                        Id = product.ProductID,
                        Name = product.ProductName,
                        Price = product.Price
                    })
                .ToList();
        }

        public void RemoveItemsByComboId(int comboId)
        {
            var items = _context.ComboProducts
                .Where(x => x.ProductID == comboId)
                .ToList();

            _context.ComboProducts.RemoveRange(items);
        }

        public void AddItems(int comboId, IReadOnlyList<int> productIds)
        {
            var comboItems = productIds
                .Select(productId => new ComboProduct
                {
                    ProductID = comboId,
                    ComboID = productId,
                    Quantity = 1
                })
                .ToList();

            _context.ComboProducts.AddRange(comboItems);
        }
    }
}
