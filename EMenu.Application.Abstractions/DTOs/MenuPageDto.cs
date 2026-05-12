using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.DTOs
{
    public class MenuPageDto
    {
        public IReadOnlyList<Category> Categories { get; set; } = [];

        public IReadOnlyDictionary<int, IReadOnlyList<MenuRecommendedProductDto>> RecommendedProductsByCategory { get; set; }
            = new Dictionary<int, IReadOnlyList<MenuRecommendedProductDto>>();
    }
}
