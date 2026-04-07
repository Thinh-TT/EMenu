using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class InventoryIndexViewModel
    {
        public string? Keyword { get; set; }

        public bool LowStockOnly { get; set; }

        public string SortBy { get; set; } = "name";

        public bool Descending { get; set; }

        public int LowStockCount { get; set; }

        public IReadOnlyList<Ingredient> Ingredients { get; set; } = [];
    }
}
