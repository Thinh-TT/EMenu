using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                throw new Exception("Session not found");

            if (session.Status != 1)
                throw new Exception("Session is already closed");

            var order = _context.Orders
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (order == null)
                throw new Exception("Order not found");

            if (!_context.OrderProducts.Any(x => x.OrderID == order.OrderID))
                throw new Exception("Order has no items");

            var existingInvoice = _context.Invoices
                .FirstOrDefault(x => x.OrderID == order.OrderID);

            if (existingInvoice != null)
                throw new Exception("Order is already paid");

            var invoice = new Invoice
            {
                OrderID = order.OrderID,
                CreatedDate = DateTime.Now,
                TotalAmount = order.TotalAmount
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

            order.Status = OrderStatus.Completed;
            session.Status = 0;
            session.EndTime = DateTime.Now;

            var table = _context.RestaurantTables
                .FirstOrDefault(x => x.TableID == session.TableID);

            if (table == null)
                throw new Exception("Table not found");

            table.Status = 0;

            _context.SaveChanges();
        }
        public void EndSession(int sessionId)
        {
            var session = _context.OrderSessions.Find(sessionId);

            if (session == null)
                throw new Exception("Session not found");

            session.Status = 0;
            session.EndTime = DateTime.Now;

            var table = _context.RestaurantTables.Find(session.TableID);

            if (table == null)
                throw new Exception("Table not found");

            table.Status = 0;
        }
        public void PaymentSuccess(int orderId)
        {
            var invoice = new Invoice
            {
                OrderID = orderId,
                CreatedDate = DateTime.Now
            };

            _context.Invoices.Add(invoice);

            var order = _context.Orders
                .FirstOrDefault(x => x.OrderID == orderId);

            if (order == null)
                throw new Exception("Order not found");

            EndSession(order.OrderSessionID);

            _context.SaveChanges();
        }
    }
}
