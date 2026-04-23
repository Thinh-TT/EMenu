using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IOrderRepository
    {
        Order? GetById(int orderId);
        Order? GetByIdWithDetails(int orderId);
        Order? GetLatestBySession(int sessionId);
        Order? GetEditableBySession(int sessionId);
        IReadOnlyList<Order> GetBySessionWithDetails(int sessionId);
        int CountByCreatedDate(DateTime date);
        bool HasInvoice(int orderId);
        bool HasInvoicedOrder(int sessionId);
        bool HasUnpaidBillableOrder(int sessionId);
        int ReassignSession(int sourceSessionId, int targetSessionId);
        void Add(Order order);
        void AddOrderProduct(OrderProduct orderProduct);
    }
}
