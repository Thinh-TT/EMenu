using EMenu.Domain.Enums;

namespace EMenu.Application.Abstractions.DTOs
{
    public class MenuRecommendedProductDto
    {
        public int CategoryId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public ProductType ProductType { get; set; }

        public int QuantitySold { get; set; }
    }
}
