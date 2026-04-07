using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IIngredientProductRepository
    {
        IReadOnlyList<IngredientProduct> GetAllWithDetails();
        IReadOnlyList<IngredientProduct> GetByProductId(int productId);
        IReadOnlyList<IngredientProduct> GetByIngredientId(int ingredientId);
        IngredientProduct? GetById(int id);
        IngredientProduct? GetByProductAndIngredient(int productId, int ingredientId);
        void Add(IngredientProduct ingredientProduct);
        void Update(IngredientProduct ingredientProduct);
        void Remove(IngredientProduct ingredientProduct);
    }
}
