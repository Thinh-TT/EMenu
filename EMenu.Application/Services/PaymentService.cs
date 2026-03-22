using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;

namespace EMenu.Application.Services
{
    public class PaymentService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentService(
            ISessionRepository sessionRepository,
            IOrderRepository orderRepository,
            IOrderItemRepository orderItemRepository,
            ITableRepository tableRepository,
            IPaymentRepository paymentRepository,
            IUnitOfWork unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _tableRepository = tableRepository;
            _paymentRepository = paymentRepository;
            _unitOfWork = unitOfWork;
        }

        public void PayCash(int sessionId)
        {
            var session = _sessionRepository.GetById(sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            if (session.Status != 1)
                throw new InvalidOperationException("Session is already closed.");

            var order = _orderRepository.GetLatestBySession(sessionId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            var items = _orderItemRepository.GetBillableByOrderId(order.OrderID);

            if (!items.Any())
                throw new InvalidOperationException("Order has no billable items.");

            var existingInvoice = _paymentRepository.GetInvoiceByOrderId(order.OrderID);

            if (existingInvoice != null)
                throw new InvalidOperationException("Order is already paid.");

            var table = _tableRepository.GetById(session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            var totalAmount = items.Sum(x => x.Price * x.Quantity);

            using var transaction = _unitOfWork.BeginTransaction();

            order.TotalAmount = totalAmount;
            order.Status = OrderStatus.Completed;

            var invoice = new Invoice
            {
                OrderID = order.OrderID,
                CreatedDate = DateTime.Now,
                TotalAmount = totalAmount
            };

            _paymentRepository.AddInvoice(invoice);

            var payment = new Payment
            {
                Invoice = invoice,
                Method = "Cash",
                Amount = invoice.TotalAmount,
                Status = 1,
                PaymentTime = DateTime.Now
            };

            _paymentRepository.AddPayment(payment);

            CloseSession(session, table);

            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        public void EndSession(int sessionId)
        {
            var session = _sessionRepository.GetById(sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            var table = _tableRepository.GetById(session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            using var transaction = _unitOfWork.BeginTransaction();

            CloseSession(session, table);

            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        public void PaymentSuccess(int orderId)
        {
            var order = _orderRepository.GetById(orderId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            if (_paymentRepository.GetInvoiceByOrderId(orderId) != null)
                return;

            var items = _orderItemRepository.GetBillableByOrderId(orderId);
            var totalAmount = items.Sum(x => x.Price * x.Quantity);

            using var transaction = _unitOfWork.BeginTransaction();

            var invoice = new Invoice
            {
                OrderID = orderId,
                CreatedDate = DateTime.Now,
                TotalAmount = totalAmount
            };

            _paymentRepository.AddInvoice(invoice);
            _paymentRepository.AddPayment(new Payment
            {
                Invoice = invoice,
                Method = "VNPay",
                Amount = totalAmount,
                Status = 1,
                PaymentTime = DateTime.Now
            });

            order.TotalAmount = totalAmount;
            order.Status = OrderStatus.Completed;

            var session = _sessionRepository.GetById(order.OrderSessionID);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            var table = _tableRepository.GetById(session.TableID);

            if (table == null)
                throw new InvalidOperationException("Table not found.");

            CloseSession(session, table);

            _unitOfWork.SaveChanges();

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
