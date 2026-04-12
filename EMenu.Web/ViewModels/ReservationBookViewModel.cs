using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class ReservationBookViewModel
    {
        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public string? CustomerEmail { get; set; }

        public int? TableId { get; set; }

        public DateTime ReservationTime { get; set; } = DateTime.Now.AddHours(1);

        public int NumberOfGuests { get; set; } = 2;

        public IReadOnlyList<RestaurantTable> Tables { get; set; } = [];
    }
}
