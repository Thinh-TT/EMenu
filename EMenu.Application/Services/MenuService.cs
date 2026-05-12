using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Repositories;

namespace EMenu.Application.Services
{
    public class MenuService
    {
        private const int DefaultRecommendedProductsPerCategory = 3;

        private readonly ICategoryRepository _categoryRepository;
        private readonly IOrderItemRepository _orderItemRepository;

        public MenuService(
            ICategoryRepository categoryRepository,
            IOrderItemRepository orderItemRepository)
        {
            _categoryRepository = categoryRepository;
            _orderItemRepository = orderItemRepository;
        }

        public MenuPageDto GetMenu()
        {
            var categories = _categoryRepository.GetAllWithProducts().ToList();
            var recommendedProductsByCategory = _orderItemRepository
                .GetTopProductsByCategory(DefaultRecommendedProductsPerCategory)
                .GroupBy(x => x.CategoryId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<MenuRecommendedProductDto>)group.ToList());

            return new MenuPageDto
            {
                Categories = categories,
                RecommendedProductsByCategory = recommendedProductsByCategory
            };
        }
    }
}
