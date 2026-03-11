using EMenu.Domain.Entities;
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
            var order = _context.Orders
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (order == null)
                throw new Exception("Order not found");

            // create invoice
            var invoice = new Invoice
            {
                OrderID = order.OrderID,
                CreatedDate = DateTime.Now,
                TotalAmount = order.TotalAmount
            };

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            // create payment
            var payment = new Payment
            {
                InvoiceID = invoice.InvoiceID,
                Method = "Cash",
                Amount = invoice.TotalAmount,
                Status = 1,
                PaymentTime = DateTime.Now
            };

            _context.Payments.Add(payment);

            // end session
            var session = _context.OrderSessions
                .First(x => x.OrderSessionID == sessionId);

            session.Status = 0;
            session.EndTime = DateTime.Now;

            // set table available
            var table = _context.RestaurantTables
                .First(x => x.TableID == session.TableID);

            table.Status = 0;

            _context.SaveChanges();
        }
        public void EndSession(int sessionId)
        {
            var session = _context.OrderSessions.Find(sessionId);

            session.Status = 0;
            session.EndTime = DateTime.Now;

            var table = _context.RestaurantTables.Find(session.TableID);

            table.Status = 0;
        }
        public void PaymentSuccess(int OrderID)
        {
            var invoice = new Invoice
            {
                OrderID = OrderID,
                CreatedDate = DateTime.Now
            };

            _context.Invoices.Add(invoice);

            EndSession(OrderID);

            _context.SaveChanges();
        }
    }
}
