namespace EMenu.Application.Abstractions.DTOs
{
    public class ProductFilterDto
    {
        public string? Name { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? ProductType { get; set; }
    }
}
