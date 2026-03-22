using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly AppDbContext _context;

        public SessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public OrderSession? GetById(int sessionId)
        {
            return _context.OrderSessions.FirstOrDefault(x => x.OrderSessionID == sessionId);
        }

        public OrderSession? GetByIdWithTable(int sessionId)
        {
            return _context.OrderSessions
                .Include(x => x.RestaurantTable)
                .FirstOrDefault(x => x.OrderSessionID == sessionId);
        }

        public OrderSession? GetActiveByTable(int tableId)
        {
            return _context.OrderSessions
                .Include(x => x.RestaurantTable)
                .FirstOrDefault(x => x.TableID == tableId && x.Status == 1);
        }

        public bool HasActiveByTable(int tableId)
        {
            return _context.OrderSessions.Any(x => x.TableID == tableId && x.Status == 1);
        }

        public void Add(OrderSession session)
        {
            _context.OrderSessions.Add(session);
        }
    }
}
