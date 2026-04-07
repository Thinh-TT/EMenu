namespace EMenu.Domain.Entities
{
    public class ReceiptIngredient
    {
        public int Id { get; set; }

        public int ReceiptID { get; set; }

        public int IngredientID { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public Receipt Receipt { get; set; }

        public Ingredient Ingredient { get; set; }
    }
}
