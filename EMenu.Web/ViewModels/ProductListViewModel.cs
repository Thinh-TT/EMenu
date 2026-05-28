using EMenu.Domain.Entities;

namespace EMenu.Web.ViewModels
{
    public class ProductListViewModel
    {
        public string? SearchName { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? ProductType { get; set; }

        public IReadOnlyList<Category> Categories { get; set; } = [];
        public IReadOnlyList<Product> Products { get; set; } = [];
    }
}
