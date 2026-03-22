using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;

namespace EMenu.Application.Services
{
    public class ComboService
    {
        private readonly IProductRepository _productRepository;
        private readonly IComboRepository _comboRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ComboService(
            IProductRepository productRepository,
            IComboRepository comboRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _comboRepository = comboRepository;
            _unitOfWork = unitOfWork;
        }

        public List<Product> GetCombos()
        {
            return _productRepository.GetByType(ProductType.Combo).ToList();
        }

        public List<Product> GetProducts()
        {
            return _productRepository.GetByType(ProductType.Single).ToList();
        }

        public void CreateCombo(Product combo, List<int> productIds)
        {
            using var transaction = _unitOfWork.BeginTransaction();

            _productRepository.Add(combo);
            _unitOfWork.SaveChanges();

            _comboRepository.AddItems(combo.ProductID, productIds);
            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        public List<Product> GetSingleProducts()
        {
            return _productRepository.GetByType(ProductType.Single).ToList();
        }

        public List<int> GetComboItemIds(int comboId)
        {
            return _comboRepository.GetComboItemIds(comboId).ToList();
        }

        public List<ComboItemDto> GetComboItems(int comboId)
        {
            return _comboRepository.GetComboItems(comboId).ToList();
        }

        public void UpdateCombo(int comboId, List<int> productIds)
        {
            using var transaction = _unitOfWork.BeginTransaction();

            _comboRepository.RemoveItemsByComboId(comboId);
            _comboRepository.AddItems(comboId, productIds);
            _unitOfWork.SaveChanges();

            transaction.Commit();
        }
    }
}
