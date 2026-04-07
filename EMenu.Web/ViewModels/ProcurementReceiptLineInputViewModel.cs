namespace EMenu.Web.ViewModels
{
    public class ProcurementReceiptLineInputViewModel
    {
        public int IngredientId { get; set; }

        public string? IngredientName { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }
    }
}
