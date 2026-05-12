using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using Moq;

namespace EMenu.Tests.Services;

public class MenuServiceTests
{
    [Fact]
    public void GetMenu_ReturnsCategoriesAndRecommendationsGroupedByCategory()
    {
        var categoryRepository = new Mock<ICategoryRepository>();
        var orderItemRepository = new Mock<IOrderItemRepository>();

        categoryRepository
            .Setup(x => x.GetAllWithProducts())
            .Returns(
            [
                new Category
                {
                    CategoryID = 1,
                    CategoryName = "Drinks",
                    Products =
                    [
                        new Product { ProductID = 1, ProductName = "Tea", ProductType = ProductType.Single },
                        new Product { ProductID = 2, ProductName = "Coffee", ProductType = ProductType.Single }
                    ]
                },
                new Category
                {
                    CategoryID = 2,
                    CategoryName = "Desserts",
                    Products =
                    [
                        new Product { ProductID = 3, ProductName = "Cake", ProductType = ProductType.Single }
                    ]
                }
            ]);

        orderItemRepository
            .Setup(x => x.GetTopProductsByCategory(3))
            .Returns(
            [
                new MenuRecommendedProductDto
                {
                    CategoryId = 1,
                    ProductId = 2,
                    ProductName = "Coffee",
                    ProductType = ProductType.Single,
                    QuantitySold = 10
                },
                new MenuRecommendedProductDto
                {
                    CategoryId = 1,
                    ProductId = 1,
                    ProductName = "Tea",
                    ProductType = ProductType.Single,
                    QuantitySold = 7
                },
                new MenuRecommendedProductDto
                {
                    CategoryId = 2,
                    ProductId = 3,
                    ProductName = "Cake",
                    ProductType = ProductType.Single,
                    QuantitySold = 5
                }
            ]);

        var service = new MenuService(
            categoryRepository.Object,
            orderItemRepository.Object);

        var result = service.GetMenu();

        Assert.Equal(2, result.Categories.Count);
        Assert.Equal("Drinks", result.Categories[0].CategoryName);
        Assert.True(result.RecommendedProductsByCategory.ContainsKey(1));
        Assert.True(result.RecommendedProductsByCategory.ContainsKey(2));
        Assert.Equal([2, 1], result.RecommendedProductsByCategory[1].Select(x => x.ProductId).ToArray());
        Assert.Equal(3, result.RecommendedProductsByCategory[2].Single().ProductId);
    }
}
