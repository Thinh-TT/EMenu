using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;

namespace EMenu.Application.Services
{
    public class PaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        public void PayCash(int sessionId)
        {
            var session = _context.OrderSessions
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            if (session.Status != 1)
                throw new InvalidOperationException("Session is already closed.");

            var order = _context.Orders
                .OrderByDescending(x => x.OrderID)
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            var items = _context.OrderProducts
                .Where(x => x.OrderID == order.OrderID && x.Status != OrderItemStatus.Cancelled)
                .ToList();

            if (!items.Any())
                throw new InvalidOperationException("Order has no billable items.");

            var existingInvoice = _context.Invoices
                .FirstOrDefault(x => x.OrderID == order.OrderID);

            if (existingInvoice != null)
                throw new InvalidOperationException("Order is already paid.");

            var table = _context.RestaurantTables
                .FirstOrDefault(x => x.TableID == session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            var totalAmount = items.Sum(x => x.Price * x.Quantity);

            using var transaction = _context.Database.BeginTransaction();

            order.TotalAmount = totalAmount;
            order.Status = OrderStatus.Completed;

            var invoice = new Invoice
            {
                OrderID = order.OrderID,
                CreatedDate = DateTime.Now,
                TotalAmount = totalAmount
            };

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var payment = new Payment
            {
                InvoiceID = invoice.InvoiceID,
                Method = "Cash",
                Amount = invoice.TotalAmount,
                Status = 1,
                PaymentTime = DateTime.Now
            };

            _context.Payments.Add(payment);

            session.Status = 0;
            session.EndTime = DateTime.Now;
            table.Status = 0;

            _context.SaveChanges();

            transaction.Commit();
        }

        public void EndSession(int sessionId)
        {
            var session = _context.OrderSessions.Find(sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            var table = _context.RestaurantTables.Find(session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            using var transaction = _context.Database.BeginTransaction();

            CloseSession(session, table);

            _context.SaveChanges();

            transaction.Commit();
        }

        public void PaymentSuccess(int orderId)
        {
            var order = _context.Orders
                .FirstOrDefault(x => x.OrderID == orderId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            if (_context.Invoices.Any(x => x.OrderID == orderId))
                return;

            var totalAmount = _context.OrderProducts
                .Where(x => x.OrderID == orderId && x.Status != OrderItemStatus.Cancelled)
                .Sum(x => x.Price * x.Quantity);

            using var transaction = _context.Database.BeginTransaction();

            var invoice = new Invoice
            {
                OrderID = orderId,
                CreatedDate = DateTime.Now,
                TotalAmount = totalAmount
            };

            _context.Invoices.Add(invoice);

            order.TotalAmount = totalAmount;
            order.Status = OrderStatus.Completed;

            var session = _context.OrderSessions
                .FirstOrDefault(x => x.OrderSessionID == order.OrderSessionID);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            var table = _context.RestaurantTables
                .FirstOrDefault(x => x.TableID == session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            CloseSession(session, table);

            _context.SaveChanges();

            transaction.Commit();
        }

        private static void CloseSession(OrderSession session, RestaurantTable table)
        {
            session.Status = 0;
            session.EndTime = DateTime.Now;
            table.Status = 0;
        }
    }
}
