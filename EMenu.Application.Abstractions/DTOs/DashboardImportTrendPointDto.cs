namespace EMenu.Application.Abstractions.DTOs
{
    public class DashboardImportTrendPointDto
    {
        public string Label { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public decimal TotalValue { get; set; }
    }
}
