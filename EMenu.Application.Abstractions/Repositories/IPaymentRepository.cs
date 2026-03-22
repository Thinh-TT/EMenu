using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IPaymentRepository
    {
        Invoice? GetInvoiceByOrderId(int orderId);
        decimal GetRevenueByDate(DateTime date);
        void AddInvoice(Invoice invoice);
        void AddPayment(Payment payment);
    }
}
