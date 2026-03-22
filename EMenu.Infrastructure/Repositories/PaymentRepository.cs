using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;

namespace EMenu.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public Invoice? GetInvoiceByOrderId(int orderId)
        {
            return _context.Invoices.FirstOrDefault(x => x.OrderID == orderId);
        }

        public decimal GetRevenueByDate(DateTime date)
        {
            var targetDate = date.Date;

            return _context.Invoices
                .Where(x => x.CreatedDate.Date == targetDate)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0;
        }

        public void AddInvoice(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
        }

        public void AddPayment(Payment payment)
        {
            _context.Payments.Add(payment);
        }
    }
}
