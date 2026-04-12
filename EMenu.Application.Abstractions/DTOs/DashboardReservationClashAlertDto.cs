namespace EMenu.Application.Abstractions.DTOs
{
    public class DashboardReservationClashAlertDto
    {
        public int TableId { get; set; }

        public string TableName { get; set; } = string.Empty;

        public DateTime ReservationTime { get; set; }

        public int ConflictCount { get; set; }
    }
}
