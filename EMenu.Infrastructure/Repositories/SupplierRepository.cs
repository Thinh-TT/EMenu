using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;

namespace EMenu.Infrastructure.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly AppDbContext _context;

        public SupplierRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<Supplier> GetAll()
        {
            return _context.Suppliers
                .OrderBy(x => x.Name)
                .ToList();
        }

        public Supplier? GetById(int supplierId)
        {
            return _context.Suppliers
                .FirstOrDefault(x => x.SupplierID == supplierId);
        }

        public Supplier? GetByName(string name)
        {
            var normalized = name.Trim().ToLowerInvariant();

            return _context.Suppliers
                .FirstOrDefault(x => x.Name.ToLower() == normalized);
        }

        public bool HasAnyReceipt(int supplierId)
        {
            return _context.Receipts.Any(x => x.SupplierID == supplierId);
        }

        public void Add(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
        }

        public void Update(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
        }

        public void Remove(Supplier supplier)
        {
            _context.Suppliers.Remove(supplier);
        }
    }
}
