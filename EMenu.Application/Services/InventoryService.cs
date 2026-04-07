using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;

namespace EMenu.Application.Services
{
    public class InventoryService
    {
        private readonly IIngredientRepository _ingredientRepository;
        private readonly IIngredientProductRepository _ingredientProductRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IUnitOfWork _unitOfWork;

        public InventoryService(
            IIngredientRepository ingredientRepository,
            IIngredientProductRepository ingredientProductRepository,
            IProductRepository productRepository,
            IOrderItemRepository orderItemRepository,
            IUnitOfWork unitOfWork)
        {
            _ingredientRepository = ingredientRepository;
            _ingredientProductRepository = ingredientProductRepository;
            _productRepository = productRepository;
            _orderItemRepository = orderItemRepository;
            _unitOfWork = unitOfWork;
        }

        public IReadOnlyList<Ingredient> GetAllIngredients()
        {
            return _ingredientRepository.GetAll();
        }

        public IReadOnlyList<Ingredient> GetLowStockIngredients()
        {
            return _ingredientRepository.GetLowStock();
        }

        public Ingredient GetIngredientById(int ingredientId)
        {
            return EnsureIngredientExists(ingredientId);
        }

        public Ingredient CreateIngredient(string name, string unit, decimal stockQuantity, decimal minStock)
        {
            ValidateIngredientValues(name, unit, stockQuantity, minStock);
            EnsureIngredientNameIsUnique(name, null);

            var ingredient = new Ingredient
            {
                Name = name.Trim(),
                Unit = unit.Trim(),
                StockQuantity = stockQuantity,
                MinStock = minStock
            };

            _ingredientRepository.Add(ingredient);
            _unitOfWork.SaveChanges();

            return ingredient;
        }

        public Ingredient UpdateIngredient(int ingredientId, string name, string unit, decimal stockQuantity, decimal minStock)
        {
            ValidateIngredientValues(name, unit, stockQuantity, minStock);

            var ingredient = EnsureIngredientExists(ingredientId);
            EnsureIngredientNameIsUnique(name, ingredientId);

            ingredient.Name = name.Trim();
            ingredient.Unit = unit.Trim();
            ingredient.StockQuantity = stockQuantity;
            ingredient.MinStock = minStock;

            _ingredientRepository.Update(ingredient);
            _unitOfWork.SaveChanges();

            return ingredient;
        }

        public void DeleteIngredient(int ingredientId)
        {
            var ingredient = EnsureIngredientExists(ingredientId);
            var links = _ingredientProductRepository.GetByIngredientId(ingredientId);

            if (links.Count > 0)
            {
                throw new InvalidOperationException("Cannot delete ingredient because it is assigned to products.");
            }

            _ingredientRepository.Remove(ingredient);
            _unitOfWork.SaveChanges();
        }

        public IReadOnlyList<Product> GetAllProducts()
        {
            return _productRepository.GetAllWithCategory();
        }

        public IReadOnlyList<IngredientProduct> GetAllIngredientProducts()
        {
            return _ingredientProductRepository.GetAllWithDetails();
        }

        public IReadOnlyList<IngredientProduct> GetIngredientsByProduct(int productId)
        {
            EnsureProductExists(productId);

            return _ingredientProductRepository.GetByProductId(productId);
        }

        public IReadOnlyList<IngredientProduct> GetProductsByIngredient(int ingredientId)
        {
            EnsureIngredientExists(ingredientId);

            return _ingredientProductRepository.GetByIngredientId(ingredientId);
        }

        public IngredientProduct UpsertIngredientProduct(int productId, int ingredientId, decimal quantity)
        {
            if (quantity <= 0)
            {
                throw new InvalidOperationException("Ingredient quantity must be greater than zero.");
            }

            EnsureProductExists(productId);
            EnsureIngredientExists(ingredientId);

            var existing = _ingredientProductRepository.GetByProductAndIngredient(productId, ingredientId);

            if (existing == null)
            {
                var ingredientProduct = new IngredientProduct
                {
                    ProductID = productId,
                    IngredientID = ingredientId,
                    Quantity = quantity
                };

                _ingredientProductRepository.Add(ingredientProduct);
                _unitOfWork.SaveChanges();

                return ingredientProduct;
            }

            existing.Quantity = quantity;
            _ingredientProductRepository.Update(existing);
            _unitOfWork.SaveChanges();

            return existing;
        }

        public Ingredient AddStock(int ingredientId, decimal quantity)
        {
            if (quantity <= 0)
            {
                throw new InvalidOperationException("Import quantity must be greater than zero.");
            }

            var ingredient = EnsureIngredientExists(ingredientId);
            ingredient.StockQuantity += quantity;

            _ingredientRepository.Update(ingredient);
            _unitOfWork.SaveChanges();

            return ingredient;
        }

        public bool DeductStockForServedOrderItem(int orderProductId)
        {
            var orderItem = _orderItemRepository.GetById(orderProductId);

            if (orderItem == null ||
                orderItem.Status != OrderItemStatus.Served ||
                orderItem.Quantity <= 0)
            {
                return false;
            }

            DeductStockForProduct(orderItem.ProductID, orderItem.Quantity);
            _unitOfWork.SaveChanges();

            return true;
        }

        public void RemoveIngredientProduct(int id)
        {
            var link = _ingredientProductRepository.GetById(id);

            if (link == null)
            {
                return;
            }

            _ingredientProductRepository.Remove(link);
            _unitOfWork.SaveChanges();
        }

        private Ingredient EnsureIngredientExists(int ingredientId)
        {
            var ingredient = _ingredientRepository.GetById(ingredientId);

            if (ingredient == null)
            {
                throw new InvalidOperationException("Ingredient not found.");
            }

            return ingredient;
        }

        private Product EnsureProductExists(int productId)
        {
            var product = _productRepository.GetById(productId);

            if (product == null)
            {
                throw new InvalidOperationException("Product not found.");
            }

            return product;
        }

        private void EnsureIngredientNameIsUnique(string name, int? ignoredIngredientId)
        {
            var existing = _ingredientRepository.GetByName(name);

            if (existing == null)
            {
                return;
            }

            if (ignoredIngredientId.HasValue && existing.IngredientID == ignoredIngredientId.Value)
            {
                return;
            }

            throw new InvalidOperationException("Ingredient name already exists.");
        }

        private void DeductStockForProduct(int productId, int orderQuantity)
        {
            var recipeItems = _ingredientProductRepository.GetByProductId(productId);

            foreach (var recipeItem in recipeItems)
            {
                var ingredient = _ingredientRepository.GetById(recipeItem.IngredientID);

                if (ingredient == null)
                {
                    continue;
                }

                ingredient.StockQuantity -= recipeItem.Quantity * orderQuantity;
                _ingredientRepository.Update(ingredient);
            }
        }

        private static void ValidateIngredientValues(string name, string unit, decimal stockQuantity, decimal minStock)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Ingredient name is required.");
            }

            if (string.IsNullOrWhiteSpace(unit))
            {
                throw new InvalidOperationException("Ingredient unit is required.");
            }

            if (stockQuantity < 0)
            {
                throw new InvalidOperationException("Stock quantity cannot be negative.");
            }

            if (minStock < 0)
            {
                throw new InvalidOperationException("Min stock cannot be negative.");
            }
        }
    }
}
