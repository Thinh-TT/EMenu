using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class CategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public List<Category> GetAll()
        {
            return _categoryRepository.GetAll().ToList();
        }

        public Category GetById(int id)
        {
            return _categoryRepository.GetById(id);
        }

        public void Create(Category category)
        {
            _categoryRepository.Add(category);
            _unitOfWork.SaveChanges();
        }

        public void Update(Category category)
        {
            _categoryRepository.Update(category);
            _unitOfWork.SaveChanges();
        }

        public void Delete(int id)
        {
            var category = _categoryRepository.GetById(id);

            if (category == null)
                return;

            _categoryRepository.Remove(category);
            _unitOfWork.SaveChanges();
        }
    }
}
