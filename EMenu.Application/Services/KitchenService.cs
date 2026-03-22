using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Enums;

namespace EMenu.Application.Services
{
    public class KitchenService
    {
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IUnitOfWork _unitOfWork;

        public KitchenService(
            IOrderItemRepository orderItemRepository,
            IUnitOfWork unitOfWork)
        {
            _orderItemRepository = orderItemRepository;
            _unitOfWork = unitOfWork;
        }

        public IReadOnlyList<KitchenPendingItemDto> GetPendingItems()
        {
            return _orderItemRepository.GetPendingKitchenItems();
        }

        public void UpdateStatus(int orderProductId, OrderItemStatus status)
        {
            var item = _orderItemRepository.GetById(orderProductId);

            if (item == null)
                throw new InvalidOperationException("Order item not found.");

            if (!IsValidStatusTransition(item.Status, status))
                throw new InvalidOperationException("Invalid kitchen status transition.");

            item.Status = status;

            _unitOfWork.SaveChanges();
        }

        private static bool IsValidStatusTransition(OrderItemStatus currentStatus, OrderItemStatus nextStatus)
        {
            if (currentStatus == nextStatus)
                return true;

            if (currentStatus == OrderItemStatus.Pending && nextStatus == OrderItemStatus.Preparing)
                return true;

            if (currentStatus == OrderItemStatus.Preparing && nextStatus == OrderItemStatus.Ready)
                return true;

            if (currentStatus == OrderItemStatus.Ready && nextStatus == OrderItemStatus.Served)
                return true;

            if (nextStatus == OrderItemStatus.Cancelled &&
                currentStatus != OrderItemStatus.Served &&
                currentStatus != OrderItemStatus.Cancelled)
                return true;

            return false;
        }
    }
}
