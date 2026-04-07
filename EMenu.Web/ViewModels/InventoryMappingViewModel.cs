using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class InventoryMappingViewModel
    {
        public int? SelectedProductId { get; set; }

        public IReadOnlyList<Product> Products { get; set; } = [];

        public IReadOnlyList<Ingredient> Ingredients { get; set; } = [];

        public IReadOnlyList<IngredientProduct> Mappings { get; set; } = [];
    }
}
