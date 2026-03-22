using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface ICategoryRepository
    {
        IReadOnlyList<Category> GetAll();
        IReadOnlyList<Category> GetAllWithProducts();
        Category? GetById(int categoryId);
        void Add(Category category);
        void Update(Category category);
        void Remove(Category category);
    }
}
