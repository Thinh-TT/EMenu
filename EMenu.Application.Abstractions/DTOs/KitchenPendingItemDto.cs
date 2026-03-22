using EMenu.Domain.Enums;

namespace EMenu.Application.Abstractions.DTOs
{
    public class KitchenPendingItemDto
    {
        public int OrderProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public OrderItemStatus Status { get; set; }

        public int OrderId { get; set; }
    }
}
