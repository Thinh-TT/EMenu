using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IIngredientRepository
    {
        IReadOnlyList<Ingredient> GetAll();
        IReadOnlyList<Ingredient> GetLowStock();
        Ingredient? GetById(int ingredientId);
        Ingredient? GetByName(string name);
        void Add(Ingredient ingredient);
        void Update(Ingredient ingredient);
        void Remove(Ingredient ingredient);
    }
}
