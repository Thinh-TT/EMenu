namespace EMenu.Application.Abstractions.DTOs
{
    public class DashboardLowStockAlertDto
    {
        public int IngredientId { get; set; }

        public string IngredientName { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public decimal StockQuantity { get; set; }

        public decimal MinStock { get; set; }
    }
}
