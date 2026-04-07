namespace EMenu.Domain.Entities
{
    public class Ingredient
    {
        public int IngredientID { get; set; }

        public string Name { get; set; }

        public string Unit { get; set; }

        public decimal StockQuantity { get; set; }

        public decimal MinStock { get; set; }

        public ICollection<IngredientProduct> IngredientProducts { get; set; }

        public ICollection<ReceiptIngredient> ReceiptIngredients { get; set; }
    }
}
