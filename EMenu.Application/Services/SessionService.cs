using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class SessionService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ITableRepository _tableRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SessionService(
            ISessionRepository sessionRepository,
            ITableRepository tableRepository,
            ICustomerRepository customerRepository,
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _tableRepository = tableRepository;
            _customerRepository = customerRepository;
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
        }

        public OrderSession GetById(int sessionId)
        {
            return _sessionRepository.GetById(sessionId);
        }

        public OrderSession GetActiveSessionByTable(int tableId)
        {
            return _sessionRepository.GetActiveByTable(tableId);
        }

        public OrderSession StartSession(int tableId, int customerId)
        {
            var table = _tableRepository.GetById(tableId);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            var customerExists = _customerRepository.Exists(customerId);

            if (!customerExists)
                throw new InvalidOperationException("Customer not found.");

            var hasActiveSession = _sessionRepository.HasActiveByTable(tableId);

            if (table.Status == 1 || hasActiveSession)
                throw new InvalidOperationException("Table is already occupied.");

            using var transaction = _unitOfWork.BeginTransaction();

            table.Status = 1;

            var session = new OrderSession
            {
                TableID = tableId,
                CustomerID = customerId,
                StartTime = DateTime.Now,
                Status = 1
            };

            _sessionRepository.Add(session);
            _unitOfWork.SaveChanges();

            transaction.Commit();

            return session;
        }

        public void EndSessionByTable(int tableId)
        {
            var session = _sessionRepository.GetActiveByTable(tableId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            EndSessionById(session.OrderSessionID);
        }

        public void EndSessionById(int sessionId)
        {
            var session = _sessionRepository.GetById(sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            if (session.Status == 0)
                return;

            EnsureSessionCanClose(sessionId);

            var table = _tableRepository.GetById(session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            using var transaction = _unitOfWork.BeginTransaction();

            session.Status = 0;
            session.EndTime = DateTime.Now;
            table.Status = 0;

            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        private void EnsureSessionCanClose(int sessionId)
        {
            var unpaidOrderExists = _orderRepository.HasUnpaidBillableOrder(sessionId);

            if (unpaidOrderExists)
                throw new InvalidOperationException("Cannot close session with unpaid order.");
        }
    }
}
