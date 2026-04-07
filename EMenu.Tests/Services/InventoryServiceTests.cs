using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using Moq;

namespace EMenu.Tests.Services;

public class InventoryServiceTests
{
    [Fact]
    public void AddStock_ValidQuantity_IncreasesStockAndSaves()
    {
        var ingredientRepository = new Mock<IIngredientRepository>();
        var ingredientProductRepository = new Mock<IIngredientProductRepository>();
        var productRepository = new Mock<IProductRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var ingredient = new Ingredient
        {
            IngredientID = 10,
            Name = "Sugar",
            Unit = "kg",
            StockQuantity = 5m,
            MinStock = 1m
        };

        ingredientRepository.Setup(x => x.GetById(10)).Returns(ingredient);

        var service = new InventoryService(
            ingredientRepository.Object,
            ingredientProductRepository.Object,
            productRepository.Object,
            orderItemRepository.Object,
            unitOfWork.Object);

        var result = service.AddStock(10, 2.5m);

        Assert.Equal(7.5m, result.StockQuantity);
        ingredientRepository.Verify(x => x.Update(ingredient), Times.Once);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
    }

    [Fact]
    public void GetLowStockIngredients_ReturnsRepositoryResult()
    {
        var ingredientRepository = new Mock<IIngredientRepository>();
        var ingredientProductRepository = new Mock<IIngredientProductRepository>();
        var productRepository = new Mock<IProductRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        ingredientRepository
            .Setup(x => x.GetLowStock())
            .Returns(new List<Ingredient>
            {
                new()
                {
                    IngredientID = 1,
                    Name = "Milk",
                    Unit = "L",
                    StockQuantity = 2m,
                    MinStock = 3m
                }
            });

        var service = new InventoryService(
            ingredientRepository.Object,
            ingredientProductRepository.Object,
            productRepository.Object,
            orderItemRepository.Object,
            unitOfWork.Object);

        var result = service.GetLowStockIngredients();

        Assert.Single(result);
        Assert.Equal("Milk", result[0].Name);
    }

    [Fact]
    public void DeductStockForServedOrderItem_WhenServed_DeductsRecipeAndSaves()
    {
        var ingredientRepository = new Mock<IIngredientRepository>();
        var ingredientProductRepository = new Mock<IIngredientProductRepository>();
        var productRepository = new Mock<IProductRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var flour = new Ingredient
        {
            IngredientID = 3,
            Name = "Flour",
            Unit = "kg",
            StockQuantity = 10m,
            MinStock = 2m
        };

        orderItemRepository.Setup(x => x.GetById(15))
            .Returns(new OrderProduct
            {
                OrderProductID = 15,
                ProductID = 9,
                Quantity = 2,
                Status = OrderItemStatus.Served
            });

        ingredientProductRepository.Setup(x => x.GetByProductId(9))
            .Returns(new List<IngredientProduct>
            {
                new()
                {
                    Id = 1,
                    ProductID = 9,
                    IngredientID = 3,
                    Quantity = 1.5m
                }
            });

        ingredientRepository.Setup(x => x.GetById(3)).Returns(flour);

        var service = new InventoryService(
            ingredientRepository.Object,
            ingredientProductRepository.Object,
            productRepository.Object,
            orderItemRepository.Object,
            unitOfWork.Object);

        var updated = service.DeductStockForServedOrderItem(15);

        Assert.True(updated);
        Assert.Equal(7m, flour.StockQuantity);
        ingredientRepository.Verify(x => x.Update(flour), Times.Once);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Once);
    }
}
