using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IReceiptRepository
    {
        IReadOnlyList<Receipt> GetByFilter(DateTime? fromDate, DateTime? toDate, int? supplierId);
        Receipt? GetByIdWithDetails(int receiptId);
        void Add(Receipt receipt);
        void AddIngredient(ReceiptIngredient receiptIngredient);
    }
}
