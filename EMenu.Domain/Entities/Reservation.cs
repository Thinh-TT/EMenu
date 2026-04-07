namespace EMenu.Domain.Entities
{
    public class Reservation
    {
        public int ReservationID { get; set; }

        public int CustomerID { get; set; }

        public int TableID { get; set; }

        public DateTime ReservationTime { get; set; }

        public int NumberOfGuests { get; set; }

        public int Status { get; set; }

        public Customer Customer { get; set; }

        public RestaurantTable RestaurantTable { get; set; }
    }
}
