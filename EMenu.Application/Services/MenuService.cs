using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class MenuService
    {
        private readonly ICategoryRepository _categoryRepository;

        public MenuService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public List<Category> GetMenu()
        {
            return _categoryRepository.GetAllWithProducts().ToList();
        }
    }
}
