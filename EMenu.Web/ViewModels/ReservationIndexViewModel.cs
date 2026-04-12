using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class ReservationIndexViewModel
    {
        public DateTime FromDate { get; set; } = DateTime.Today;

        public DateTime ToDate { get; set; } = DateTime.Today.AddDays(7);

        public int? TableId { get; set; }

        public int? Status { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public string? CustomerEmail { get; set; }

        public int? CreateTableId { get; set; }

        public DateTime ReservationTime { get; set; } = DateTime.Now.AddHours(1);

        public int NumberOfGuests { get; set; } = 2;

        public IReadOnlyList<RestaurantTable> Tables { get; set; } = [];

        public IReadOnlyList<Reservation> Reservations { get; set; } = [];
    }
}
