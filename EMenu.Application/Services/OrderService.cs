using EMenu.Application.DTOs;
using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;

namespace EMenu.Application.Services
{
    public class OrderService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(
            ISessionRepository sessionRepository,
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IStaffRepository staffRepository,
            IUnitOfWork unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _staffRepository = staffRepository;
            _unitOfWork = unitOfWork;
        }

        public Order CreateOrder(int sessionId, int staffId)
        {
            var session = _sessionRepository.GetById(sessionId);

            if (session == null || session.Status != 1)
                throw new InvalidOperationException("Session is not active.");

            var existingOrder = GetEditableOrder(sessionId);

            if (existingOrder != null)
                return existingOrder;

            var order = new Order
            {
                OrderSessionID = sessionId,
                StaffID = ResolveStaffId(staffId),
                CreatedTime = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = 0
            };

            _orderRepository.Add(order);
            _unitOfWork.SaveChanges();

            return order;
        }

        public void AddProduct(int sessionId, int productId, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0.");

            var session = _sessionRepository.GetById(sessionId);

            if (session == null || session.Status != 1)
                throw new InvalidOperationException("Session is not active.");

            var product = _productRepository.GetById(productId);

            if (product == null)
                throw new InvalidOperationException("Product not found.");

            if (!product.IsAvailable)
                throw new InvalidOperationException("Product is not available.");

            using var transaction = _unitOfWork.BeginTransaction();

            var order = GetEditableOrder(sessionId);

            if (order == null)
            {
                order = new Order
                {
                    OrderSessionID = sessionId,
                    StaffID = ResolveStaffId(),
                    CreatedTime = DateTime.Now,
                    Status = OrderStatus.Pending,
                    TotalAmount = 0
                };

                _orderRepository.Add(order);
            }

            if (_orderRepository.HasInvoice(order.OrderID))
                throw new InvalidOperationException("Order is already paid.");

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Order can no longer be modified.");

            var orderProduct = new OrderProduct
            {
                Order = order,
                ProductID = productId,
                Quantity = quantity,
                Price = product.Price,
                Status = OrderItemStatus.Pending
            };

            _orderRepository.AddOrderProduct(orderProduct);
            order.TotalAmount += orderProduct.Price * orderProduct.Quantity;
            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        public void SubmitOrder(int sessionId, IEnumerable<CartItemDto> items)
        {
            var cartItems = items?.ToList() ?? new List<CartItemDto>();

            if (!cartItems.Any())
                throw new InvalidOperationException("Cart is empty.");

            var session = _sessionRepository.GetById(sessionId);

            if (session == null || session.Status != 1)
                throw new InvalidOperationException("Session is not active.");

            using var transaction = _unitOfWork.BeginTransaction();

            var order = GetEditableOrder(sessionId);

            if (order == null)
            {
                order = new Order
                {
                    OrderSessionID = sessionId,
                    StaffID = ResolveStaffId(),
                    CreatedTime = DateTime.Now,
                    Status = OrderStatus.Pending,
                    TotalAmount = 0
                };

                _orderRepository.Add(order);
            }

            if (_orderRepository.HasInvoice(order.OrderID))
                throw new InvalidOperationException("Order is already paid.");

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Order can no longer be modified.");

            decimal addedAmount = 0;

            foreach (var item in cartItems)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException("Quantity must be greater than 0.");

                var product = _productRepository.GetById(item.ProductId);

                if (product == null)
                    throw new InvalidOperationException("Product not found.");

                if (!product.IsAvailable)
                    throw new InvalidOperationException("Product is not available.");

                var orderProduct = new OrderProduct
                {
                    Order = order,
                    ProductID = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price,
                    Status = OrderItemStatus.Pending
                };

                _orderRepository.AddOrderProduct(orderProduct);
                addedAmount += orderProduct.Price * orderProduct.Quantity;
            }

            order.TotalAmount += addedAmount;
            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        public BillDto GetSessionBill(int sessionId)
        {
            var session = _sessionRepository.GetByIdWithTable(sessionId);

            if (session == null)
                throw new InvalidOperationException("Session not found.");

            var orders = _orderRepository.GetBySessionWithDetails(sessionId);

            if (!orders.Any())
                throw new InvalidOperationException("Order not found.");

            var items = orders
                .SelectMany(o => o.OrderProducts)
                .Where(x => x.Status != OrderItemStatus.Cancelled)
                .Select(p => new BillItemDto
                {
                    ProductName = p.Product.ProductName,
                    Quantity = p.Quantity,
                    UnitPrice = p.Price
                })
                .ToList();

            if (!items.Any())
                throw new InvalidOperationException("Order has no billable items.");

            return new BillDto
            {
                TableName = session.RestaurantTable?.TableName,
                Items = items,
                TotalAmount = items.Sum(x => x.UnitPrice * x.Quantity)
            };
        }

        private Order? GetEditableOrder(int sessionId)
        {
            return _orderRepository.GetEditableBySession(sessionId);
        }

        private int ResolveStaffId(int? staffId = null)
        {
            if (staffId.HasValue && staffId.Value > 0)
            {
                var staff = _staffRepository.GetById(staffId.Value);

                if (staff == null)
                    throw new InvalidOperationException("Staff not found.");

                return staff.StaffID;
            }

            var systemStaff = _staffRepository.GetSystemStaff();

            if (systemStaff != null)
                return systemStaff.StaffID;

            var firstStaff = _staffRepository.GetFirstStaff();

            if (firstStaff == null)
                throw new InvalidOperationException("No staff account configured.");

            return firstStaff.StaffID;
        }
    }
}
