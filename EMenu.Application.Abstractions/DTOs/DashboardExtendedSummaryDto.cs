namespace EMenu.Application.Abstractions.DTOs
{
    public class DashboardExtendedSummaryDto
    {
        public decimal TodayRevenue { get; set; }

        public int OrdersToday { get; set; }

        public int TablesInUse { get; set; }

        public int LowStockCount { get; set; }

        public decimal ImportValueToday { get; set; }

        public decimal ImportValueThisMonth { get; set; }

        public int ReservationsToday { get; set; }

        public decimal StaffHoursToday { get; set; }
    }
}
