using EMenu.Application.Abstractions.DTOs;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IProductRepository
    {
        IReadOnlyList<Product> GetAll();
        IReadOnlyList<Product> GetAllWithCategory();
        IReadOnlyList<Product> GetByType(ProductType productType);
        IReadOnlyList<Product> GetFiltered(ProductFilterDto filter);
        Product? GetById(int productId);
        void Add(Product product);
        void Update(Product product);
        void Remove(Product product);
    }
}
