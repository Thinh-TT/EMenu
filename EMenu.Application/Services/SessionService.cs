using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public OrderSession StartSession(int tableId, int customerId )
        {
            var table = _context.RestaurantTables.Find(tableId);

            if (table == null)
                throw new Exception("Table not found");

            if (table.Status == 1)
                throw new Exception("Table is already occupied");

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

            return session;
        }

        public void EndSession(int tableId)
        {
            var session = _context.OrderSessions
                .FirstOrDefault(x => x.TableID == tableId && x.Status == 1);

            if (session == null)
                throw new Exception("Session not found");

            session.Status = 0;
            session.EndTime = DateTime.Now;

            var table = _context.RestaurantTables
                .FirstOrDefault(x => x.TableID == tableId);

            table.Status = 0;

            _context.SaveChanges();
        }

    }
}
