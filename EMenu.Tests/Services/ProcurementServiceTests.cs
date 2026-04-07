using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Application.Services;
using EMenu.Domain.Entities;
using Moq;

namespace EMenu.Tests.Services;

public class ProcurementServiceTests
{
    [Fact]
    public void CreateReceipt_ValidData_UpdatesStockAndReturnsTotal()
    {
        var supplierRepository = new Mock<ISupplierRepository>();
        var receiptRepository = new Mock<IReceiptRepository>();
        var ingredientRepository = new Mock<IIngredientRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        supplierRepository.Setup(x => x.GetById(1))
            .Returns(new Supplier { SupplierID = 1, Name = "Supplier A" });
        staffRepository.Setup(x => x.GetById(5))
            .Returns(new Staff { StaffID = 5, StaffName = "Staff A" });

        var sugar = new Ingredient
        {
            IngredientID = 10,
            Name = "Sugar",
            Unit = "kg",
            StockQuantity = 5m,
            MinStock = 2m
        };
        var milk = new Ingredient
        {
            IngredientID = 11,
            Name = "Milk",
            Unit = "L",
            StockQuantity = 8m,
            MinStock = 3m
        };

        ingredientRepository.Setup(x => x.GetById(10)).Returns(sugar);
        ingredientRepository.Setup(x => x.GetById(11)).Returns(milk);

        unitOfWork.Setup(x => x.BeginTransaction())
            .Returns(transaction.Object);

        receiptRepository.Setup(x => x.Add(It.IsAny<Receipt>()))
            .Callback<Receipt>(x => x.ReceiptID = 99);

        var service = new ProcurementService(
            supplierRepository.Object,
            receiptRepository.Object,
            ingredientRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var result = service.CreateReceipt(
            supplierId: 1,
            staffId: 5,
            items: new List<ProcurementReceiptItemInputDto>
            {
                new() { IngredientId = 10, Quantity = 2m, Price = 100m },
                new() { IngredientId = 11, Quantity = 1.5m, Price = 80m }
            },
            createdDate: new DateTime(2026, 4, 7, 9, 0, 0));

        Assert.Equal(99, result.ReceiptId);
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(320m, result.TotalAmount);
        Assert.Equal(7m, sugar.StockQuantity);
        Assert.Equal(9.5m, milk.StockQuantity);

        receiptRepository.Verify(x => x.AddIngredient(It.IsAny<ReceiptIngredient>()), Times.Exactly(2));
        ingredientRepository.Verify(x => x.Update(sugar), Times.Once);
        ingredientRepository.Verify(x => x.Update(milk), Times.Once);
        unitOfWork.Verify(x => x.SaveChanges(), Times.Exactly(2));
        transaction.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public void CreateReceipt_InvalidQuantity_ThrowsAndDoesNotCommit()
    {
        var supplierRepository = new Mock<ISupplierRepository>();
        var receiptRepository = new Mock<IReceiptRepository>();
        var ingredientRepository = new Mock<IIngredientRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<ITransaction>();

        supplierRepository.Setup(x => x.GetById(1))
            .Returns(new Supplier { SupplierID = 1, Name = "Supplier A" });
        staffRepository.Setup(x => x.GetById(5))
            .Returns(new Staff { StaffID = 5, StaffName = "Staff A" });
        unitOfWork.Setup(x => x.BeginTransaction())
            .Returns(transaction.Object);
        receiptRepository.Setup(x => x.Add(It.IsAny<Receipt>()))
            .Callback<Receipt>(x => x.ReceiptID = 99);

        var service = new ProcurementService(
            supplierRepository.Object,
            receiptRepository.Object,
            ingredientRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.CreateReceipt(
                supplierId: 1,
                staffId: 5,
                items: new List<ProcurementReceiptItemInputDto>
                {
                    new() { IngredientId = 10, Quantity = 0m, Price = 100m }
                }));

        Assert.Equal("Receipt ingredient quantity must be greater than zero.", ex.Message);
        transaction.Verify(x => x.Commit(), Times.Never);
    }

    [Fact]
    public void CreateReceipt_SupplierNotFound_Throws()
    {
        var supplierRepository = new Mock<ISupplierRepository>();
        var receiptRepository = new Mock<IReceiptRepository>();
        var ingredientRepository = new Mock<IIngredientRepository>();
        var staffRepository = new Mock<IStaffRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        supplierRepository.Setup(x => x.GetById(1)).Returns((Supplier?)null);

        var service = new ProcurementService(
            supplierRepository.Object,
            receiptRepository.Object,
            ingredientRepository.Object,
            staffRepository.Object,
            unitOfWork.Object);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.CreateReceipt(
                supplierId: 1,
                staffId: 5,
                items: new List<ProcurementReceiptItemInputDto>
                {
                    new() { IngredientId = 10, Quantity = 1m, Price = 10m }
                }));

        Assert.Equal("Supplier not found.", ex.Message);
        unitOfWork.Verify(x => x.BeginTransaction(), Times.Never);
    }
}
