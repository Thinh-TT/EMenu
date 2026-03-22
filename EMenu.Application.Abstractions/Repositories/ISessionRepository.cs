using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface ISessionRepository
    {
        OrderSession? GetById(int sessionId);
        OrderSession? GetByIdWithTable(int sessionId);
        OrderSession? GetActiveByTable(int tableId);
        bool HasActiveByTable(int tableId);
        void Add(OrderSession session);
    }
}
