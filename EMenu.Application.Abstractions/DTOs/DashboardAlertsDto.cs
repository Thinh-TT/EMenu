namespace EMenu.Application.Abstractions.DTOs
{
    public class DashboardAlertsDto
    {
        public IReadOnlyList<DashboardLowStockAlertDto> LowStockWarnings { get; set; } = [];

        public IReadOnlyList<DashboardReservationClashAlertDto> ReservationClashes { get; set; } = [];
    }
}
