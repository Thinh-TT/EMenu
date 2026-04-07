using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class ReceiptRepository : IReceiptRepository
    {
        private readonly AppDbContext _context;

        public ReceiptRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<Receipt> GetByFilter(DateTime? fromDate, DateTime? toDate, int? supplierId)
        {
            var query = BuildReceiptDetailsQuery();

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.CreatedDate >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedDate < toExclusive);
            }

            if (supplierId.HasValue)
            {
                query = query.Where(x => x.SupplierID == supplierId.Value);
            }

            return query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.ReceiptID)
                .ToList();
        }

        public Receipt? GetByIdWithDetails(int receiptId)
        {
            return BuildReceiptDetailsQuery()
                .FirstOrDefault(x => x.ReceiptID == receiptId);
        }

        public void Add(Receipt receipt)
        {
            _context.Receipts.Add(receipt);
        }

        public void AddIngredient(ReceiptIngredient receiptIngredient)
        {
            _context.ReceiptIngredients.Add(receiptIngredient);
        }

        private IQueryable<Receipt> BuildReceiptDetailsQuery()
        {
            return _context.Receipts
                .Include(x => x.Supplier)
                .Include(x => x.Staff)
                .Include(x => x.ReceiptIngredients)
                    .ThenInclude(x => x.Ingredient);
        }
    }
}
