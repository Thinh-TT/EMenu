using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;

namespace EMenu.Infrastructure.Repositories
{
    public class IngredientRepository : IIngredientRepository
    {
        private readonly AppDbContext _context;

        public IngredientRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<Ingredient> GetAll()
        {
            return _context.Ingredients
                .OrderBy(x => x.Name)
                .ToList();
        }

        public IReadOnlyList<Ingredient> GetLowStock()
        {
            return _context.Ingredients
                .Where(x => x.StockQuantity <= x.MinStock)
                .OrderBy(x => x.StockQuantity)
                .ToList();
        }

        public Ingredient? GetById(int ingredientId)
        {
            return _context.Ingredients
                .FirstOrDefault(x => x.IngredientID == ingredientId);
        }

        public Ingredient? GetByName(string name)
        {
            var normalized = name.Trim().ToLower();

            return _context.Ingredients
                .FirstOrDefault(x => x.Name.ToLower() == normalized);
        }

        public void Add(Ingredient ingredient)
        {
            _context.Ingredients.Add(ingredient);
        }

        public void Update(Ingredient ingredient)
        {
            _context.Ingredients.Update(ingredient);
        }

        public void Remove(Ingredient ingredient)
        {
            _context.Ingredients.Remove(ingredient);
        }
    }
}
