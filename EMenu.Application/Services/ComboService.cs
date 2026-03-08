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
    public class ComboService
    {
        private readonly AppDbContext _context;

        public ComboService(AppDbContext context)
        {
            _context = context;
        }

        public List<Product> GetCombos()
        {
            return _context.Products
                .Where(x => x.ProductType == ProductType.Combo)
                .ToList();
        }

        public List<Product> GetProducts()
        {
            return _context.Products
                .Where(x => x.ProductType == ProductType.Single)
                .ToList();
        }

        public void CreateCombo(Product combo, List<int> productIds)
        {
            _context.Products.Add(combo);
            _context.SaveChanges();

            foreach (var id in productIds)
            {
                var comboItem = new ComboProduct
                {
                    ProductID = combo.ProductID,
                    ComboID = id,
                    Quantity = 1
                };

                _context.ComboProducts.Add(comboItem);
            }

            _context.SaveChanges();
        }

        public List<Product> GetSingleProducts()
        {
            return _context.Products
                .Where(x => x.ProductType == ProductType.Single)
                .ToList();
        }

        public List<int> GetComboItemIds(int comboId)
        {
            return _context.ComboProducts
                .Where(x => x.ProductID == comboId)
                .Select(x => x.ComboID)
                .ToList();
        }

        public List<object> GetComboItems(int comboId)
        {
            return _context.ComboProducts
                .Where(x => x.ProductID == comboId)
                .Join(
                    _context.Products,
                    cp => cp.ComboID,
                    p => p.ProductID,
                    (cp, p) => new
                    {
                        id = p.ProductID,
                        name = p.ProductName,
                        price = p.Price
                    }
                )
                .ToList<object>();
        }

        public void UpdateCombo(int comboId, List<int> productIds)
        {
            var oldItems = _context.ComboProducts
                .Where(x => x.ProductID == comboId);

            _context.ComboProducts.RemoveRange(oldItems);

            foreach (var id in productIds)
            {
                var comboItem = new ComboProduct
                {
                    ProductID = comboId,
                    ComboID = id,
                    Quantity = 1
                };

                _context.ComboProducts.Add(comboItem);
            }

            _context.SaveChanges();
        }
    }
}
