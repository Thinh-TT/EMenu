using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Application.Services
{
    public class SessionService
    {
        private readonly AppDbContext _context;

        public SessionService(AppDbContext context)
        {
            _context = context;
        }

        public OrderSession GetById(int sessionId)
        {
            return _context.OrderSessions
                .FirstOrDefault(x => x.OrderSessionID == sessionId);
        }

        public OrderSession GetActiveSessionByTable(int tableId)
        {
            return _context.OrderSessions
                .Include(x => x.RestaurantTable)
                .FirstOrDefault(x => x.TableID == tableId && x.Status == 1);
        }

        public OrderSession StartSession(int tableId, int customerId)
        {
            var table = _context.RestaurantTables.Find(tableId);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            var customerExists = _context.Customers.Any(x => x.CustomerID == customerId);

            if (!customerExists)
                throw new InvalidOperationException("Customer not found.");

            var hasActiveSession = _context.OrderSessions
                .Any(x => x.TableID == tableId && x.Status == 1);

            if (table.Status == 1 || hasActiveSession)
                throw new InvalidOperationException("Table is already occupied.");

            using var transaction = _context.Database.BeginTransaction();

            table.Status = 1;

            var session = new OrderSession
            {
                TableID = tableId,
                CustomerID = customerId,
                StartTime = DateTime.Now,
                Status = 1
            };

            _context.OrderSessions.Add(session);
            _context.SaveChanges();

            transaction.Commit();

            return session;
        }

        public void EndSessionByTable(int tableId)
        {
            var session = _context.OrderSessions
                .FirstOrDefault(x => x.TableID == tableId && x.Status == 1);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            EndSessionById(session.OrderSessionID);
        }

        public void EndSessionById(int sessionId)
        {
            var session = _context.OrderSessions
                .FirstOrDefault(x => x.OrderSessionID == sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            if (session.Status == 0)
                return;

            EnsureSessionCanClose(sessionId);

            var table = _context.RestaurantTables
                .FirstOrDefault(x => x.TableID == session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            using var transaction = _context.Database.BeginTransaction();

            session.Status = 0;
            session.EndTime = DateTime.Now;
            table.Status = 0;

            _context.SaveChanges();

            transaction.Commit();
        }

        private void EnsureSessionCanClose(int sessionId)
        {
            var orderIds = _context.Orders
                .Where(x => x.OrderSessionID == sessionId)
                .Select(x => x.OrderID)
                .ToList();

            if (!orderIds.Any())
                return;

            var unpaidOrderExists = orderIds.Any(orderId =>
                _context.OrderProducts.Any(x => x.OrderID == orderId && x.Status != OrderItemStatus.Cancelled) &&
                !_context.Invoices.Any(x => x.OrderID == orderId));

            if (unpaidOrderExists)
                throw new InvalidOperationException("Cannot close session with unpaid order.");
        }
    }
}
