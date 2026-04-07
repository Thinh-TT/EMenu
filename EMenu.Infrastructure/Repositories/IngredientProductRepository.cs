using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class IngredientProductRepository : IIngredientProductRepository
    {
        private readonly AppDbContext _context;

        public IngredientProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<IngredientProduct> GetAllWithDetails()
        {
            return _context.IngredientProducts
                .Include(x => x.Product)
                .Include(x => x.Ingredient)
                .OrderBy(x => x.Product.ProductName)
                .ThenBy(x => x.Ingredient.Name)
                .ToList();
        }

        public IReadOnlyList<IngredientProduct> GetByProductId(int productId)
        {
            return _context.IngredientProducts
                .Include(x => x.Product)
                .Include(x => x.Ingredient)
                .Where(x => x.ProductID == productId)
                .OrderBy(x => x.Ingredient.Name)
                .ToList();
        }

        public IReadOnlyList<IngredientProduct> GetByIngredientId(int ingredientId)
        {
            return _context.IngredientProducts
                .Include(x => x.Product)
                .Include(x => x.Ingredient)
                .Where(x => x.IngredientID == ingredientId)
                .OrderBy(x => x.Product.ProductName)
                .ToList();
        }

        public IngredientProduct? GetById(int id)
        {
            return _context.IngredientProducts
                .Include(x => x.Product)
                .Include(x => x.Ingredient)
                .FirstOrDefault(x => x.Id == id);
        }

        public IngredientProduct? GetByProductAndIngredient(int productId, int ingredientId)
        {
            return _context.IngredientProducts
                .FirstOrDefault(x => x.ProductID == productId && x.IngredientID == ingredientId);
        }

        public void Add(IngredientProduct ingredientProduct)
        {
            _context.IngredientProducts.Add(ingredientProduct);
        }

        public void Update(IngredientProduct ingredientProduct)
        {
            _context.IngredientProducts.Update(ingredientProduct);
        }

        public void Remove(IngredientProduct ingredientProduct)
        {
            _context.IngredientProducts.Remove(ingredientProduct);
        }
    }
}
