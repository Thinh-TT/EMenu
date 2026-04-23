using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.DTOs;
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
        private const int TableStatusAvailable = 0;
        private const int TableStatusOccupied = 1;
        private const int TableStatusReserved = 2;
        private const int SessionStatusClosed = 0;
        private const int SessionStatusActive = 1;

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

            if (table.Status == TableStatusOccupied || hasActiveSession)
                throw new InvalidOperationException("Table is already occupied.");

            using var transaction = _unitOfWork.BeginTransaction();

            table.Status = TableStatusOccupied;

            var session = new OrderSession
            {
                TableID = tableId,
                CustomerID = customerId,
                StartTime = DateTime.Now,
                Status = SessionStatusActive
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

            if (session.Status == SessionStatusClosed)
                return;

            EnsureSessionCanClose(sessionId);

            var table = _tableRepository.GetById(session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            using var transaction = _unitOfWork.BeginTransaction();

            session.Status = SessionStatusClosed;
            session.EndTime = DateTime.Now;
            table.Status = TableStatusAvailable;

            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        public SessionTableOperationResultDto TransferTable(int sourceTableId, int targetTableId)
        {
            EnsureDifferentTable(sourceTableId, targetTableId);

            var sourceTable = EnsureTableExists(sourceTableId);
            var targetTable = EnsureTableExists(targetTableId);
            var sourceSession = EnsureSourceSessionIsActive(sourceTableId, sourceTable);

            if (targetTable.Status != TableStatusAvailable)
                throw new InvalidOperationException("Transfer target must be an available table.");

            if (_sessionRepository.HasActiveByTable(targetTableId))
                throw new InvalidOperationException("Target table already has an active session.");

            EnsureNoInvoicedOrder(sourceSession.OrderSessionID);

            using var transaction = _unitOfWork.BeginTransaction();

            var targetSession = new OrderSession
            {
                TableID = targetTableId,
                CustomerID = sourceSession.CustomerID,
                StartTime = DateTime.Now,
                Status = SessionStatusActive
            };

            targetTable.Status = TableStatusOccupied;
            _sessionRepository.Add(targetSession);
            _unitOfWork.SaveChanges();

            var movedOrders = _orderRepository.ReassignSession(
                sourceSession.OrderSessionID,
                targetSession.OrderSessionID);

            CloseSourceSession(sourceSession, sourceTable);
            _unitOfWork.SaveChanges();

            transaction.Commit();

            return BuildResult(
                "Transfer",
                sourceTableId,
                targetTableId,
                sourceSession.OrderSessionID,
                targetSession.OrderSessionID,
                movedOrders);
        }

        public SessionTableOperationResultDto MergeTable(int sourceTableId, int targetTableId)
        {
            EnsureDifferentTable(sourceTableId, targetTableId);

            var sourceTable = EnsureTableExists(sourceTableId);
            var targetTable = EnsureTableExists(targetTableId);
            var sourceSession = EnsureSourceSessionIsActive(sourceTableId, sourceTable);

            if (targetTable.Status == TableStatusReserved)
                throw new InvalidOperationException("Cannot merge into a reserved table.");

            if (targetTable.Status != TableStatusAvailable && targetTable.Status != TableStatusOccupied)
                throw new InvalidOperationException("Invalid target table status for merge.");

            EnsureNoInvoicedOrder(sourceSession.OrderSessionID);

            using var transaction = _unitOfWork.BeginTransaction();

            OrderSession targetSession;

            if (targetTable.Status == TableStatusOccupied)
            {
                targetSession = _sessionRepository.GetActiveByTable(targetTableId)
                    ?? throw new InvalidOperationException("Target table has no active session.");

                EnsureNoInvoicedOrder(targetSession.OrderSessionID);
            }
            else
            {
                if (_sessionRepository.HasActiveByTable(targetTableId))
                    throw new InvalidOperationException("Target table already has an active session.");

                targetSession = new OrderSession
                {
                    TableID = targetTableId,
                    CustomerID = sourceSession.CustomerID,
                    StartTime = DateTime.Now,
                    Status = SessionStatusActive
                };

                targetTable.Status = TableStatusOccupied;
                _sessionRepository.Add(targetSession);
                _unitOfWork.SaveChanges();
            }

            var movedOrders = _orderRepository.ReassignSession(
                sourceSession.OrderSessionID,
                targetSession.OrderSessionID);

            CloseSourceSession(sourceSession, sourceTable);
            _unitOfWork.SaveChanges();

            transaction.Commit();

            return BuildResult(
                "Merge",
                sourceTableId,
                targetTableId,
                sourceSession.OrderSessionID,
                targetSession.OrderSessionID,
                movedOrders);
        }

        private void EnsureSessionCanClose(int sessionId)
        {
            var unpaidOrderExists = _orderRepository.HasUnpaidBillableOrder(sessionId);

            if (unpaidOrderExists)
                throw new InvalidOperationException("Cannot close session with unpaid order.");
        }

        private RestaurantTable EnsureTableExists(int tableId)
        {
            return _tableRepository.GetById(tableId)
                ?? throw new InvalidOperationException("Table not found.");
        }

        private OrderSession EnsureSourceSessionIsActive(int sourceTableId, RestaurantTable sourceTable)
        {
            if (sourceTable.Status != TableStatusOccupied)
                throw new InvalidOperationException("Source table must be occupied.");

            return _sessionRepository.GetActiveByTable(sourceTableId)
                ?? throw new InvalidOperationException("Source table has no active session.");
        }

        private static void EnsureDifferentTable(int sourceTableId, int targetTableId)
        {
            if (sourceTableId == targetTableId)
                throw new InvalidOperationException("Source and target tables must be different.");
        }

        private void EnsureNoInvoicedOrder(int sessionId)
        {
            if (_orderRepository.HasInvoicedOrder(sessionId))
                throw new InvalidOperationException("Cannot transfer or merge sessions with invoiced orders.");
        }

        private static void CloseSourceSession(OrderSession sourceSession, RestaurantTable sourceTable)
        {
            sourceSession.Status = SessionStatusClosed;
            sourceSession.EndTime = DateTime.Now;
            sourceTable.Status = TableStatusAvailable;
        }

        private static SessionTableOperationResultDto BuildResult(
            string operation,
            int sourceTableId,
            int targetTableId,
            int sourceSessionId,
            int targetSessionId,
            int movedOrders)
        {
            return new SessionTableOperationResultDto
            {
                Operation = operation,
                SourceTableId = sourceTableId,
                TargetTableId = targetTableId,
                SourceSessionId = sourceSessionId,
                TargetSessionId = targetSessionId,
                MovedOrderCount = movedOrders
            };
        }
    }
}
