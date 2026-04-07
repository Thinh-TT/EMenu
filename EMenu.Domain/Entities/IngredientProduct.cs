namespace EMenu.Domain.Entities
{
    public class IngredientProduct
    {
        public int Id { get; set; }

        public int ProductID { get; set; }

        public int IngredientID { get; set; }

        public decimal Quantity { get; set; }

        public Product Product { get; set; }

        public Ingredient Ingredient { get; set; }
    }
}
