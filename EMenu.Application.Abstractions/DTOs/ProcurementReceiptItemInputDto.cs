namespace EMenu.Application.Abstractions.DTOs
{
    public class ProcurementReceiptItemInputDto
    {
        public int IngredientId { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }
    }
}
