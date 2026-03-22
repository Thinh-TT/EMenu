using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public List<Product> GetAll()
        {
            return _productRepository.GetAllWithCategory().ToList();
        }

        public Product GetById(int id)
        {
            return _productRepository.GetById(id);
        }

        public void Create(Product product)
        {
            _productRepository.Add(product);
            _unitOfWork.SaveChanges();
        }

        public void Update(Product product)
        {
            _productRepository.Update(product);
            _unitOfWork.SaveChanges();
        }

        public void Delete(int id)
        {
            var product = _productRepository.GetById(id);

            if (product == null)
                return;

            _productRepository.Remove(product);
            _unitOfWork.SaveChanges();
        }

        public List<Category> GetCategories()
        {
            return _categoryRepository.GetAll().ToList();
        }
    }
}
