using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface ISupplierRepository
    {
        IReadOnlyList<Supplier> GetAll();
        Supplier? GetById(int supplierId);
        Supplier? GetByName(string name);
        bool HasAnyReceipt(int supplierId);
        void Add(Supplier supplier);
        void Update(Supplier supplier);
        void Remove(Supplier supplier);
    }
}
