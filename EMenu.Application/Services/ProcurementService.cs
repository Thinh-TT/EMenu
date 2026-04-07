using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class ProcurementService
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly IReceiptRepository _receiptRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProcurementService(
            ISupplierRepository supplierRepository,
            IReceiptRepository receiptRepository,
            IIngredientRepository ingredientRepository,
            IStaffRepository staffRepository,
            IUnitOfWork unitOfWork)
        {
            _supplierRepository = supplierRepository;
            _receiptRepository = receiptRepository;
            _ingredientRepository = ingredientRepository;
            _staffRepository = staffRepository;
            _unitOfWork = unitOfWork;
        }

        public IReadOnlyList<Supplier> GetSuppliers(string? keyword = null)
        {
            var suppliers = _supplierRepository.GetAll().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalized = keyword.Trim();
                suppliers = suppliers.Where(x =>
                    x.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase));
            }

            return suppliers.ToList();
        }

        public Supplier CreateSupplier(string name, string? phone, string? email)
        {
            ValidateSupplierValues(name);
            EnsureSupplierNameIsUnique(name, ignoredSupplierId: null);

            var supplier = new Supplier
            {
                Name = name.Trim(),
                Phone = NormalizeOptionalValue(phone),
                Email = NormalizeOptionalValue(email)
            };

            _supplierRepository.Add(supplier);
            _unitOfWork.SaveChanges();

            return supplier;
        }

        public Supplier UpdateSupplier(int supplierId, string name, string? phone, string? email)
        {
            ValidateSupplierValues(name);

            var supplier = EnsureSupplierExists(supplierId);
            EnsureSupplierNameIsUnique(name, ignoredSupplierId: supplierId);

            supplier.Name = name.Trim();
            supplier.Phone = NormalizeOptionalValue(phone);
            supplier.Email = NormalizeOptionalValue(email);

            _supplierRepository.Update(supplier);
            _unitOfWork.SaveChanges();

            return supplier;
        }

        public void DeleteSupplier(int supplierId)
        {
            var supplier = EnsureSupplierExists(supplierId);

            if (_supplierRepository.HasAnyReceipt(supplierId))
            {
                throw new InvalidOperationException("Cannot delete supplier because it already has receipts.");
            }

            _supplierRepository.Remove(supplier);
            _unitOfWork.SaveChanges();
        }

        public IReadOnlyList<Receipt> GetReceiptHistory(DateTime? fromDate, DateTime? toDate, int? supplierId)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                throw new InvalidOperationException("From date cannot be later than to date.");
            }

            if (supplierId.HasValue)
            {
                EnsureSupplierExists(supplierId.Value);
            }

            return _receiptRepository.GetByFilter(fromDate, toDate, supplierId);
        }

        public ProcurementReceiptResultDto CreateReceipt(
            int supplierId,
            int staffId,
            IEnumerable<ProcurementReceiptItemInputDto> items,
            DateTime? createdDate = null)
        {
            EnsureSupplierExists(supplierId);
            EnsureStaffExists(staffId);

            var normalizedItems = NormalizeItems(items);

            if (normalizedItems.Count == 0)
            {
                throw new InvalidOperationException("Receipt must contain at least one ingredient item.");
            }

            var createdAt = createdDate ?? DateTime.Now;

            using var transaction = _unitOfWork.BeginTransaction();

            var receipt = new Receipt
            {
                SupplierID = supplierId,
                StaffID = staffId,
                CreatedDate = createdAt
            };

            _receiptRepository.Add(receipt);
            _unitOfWork.SaveChanges();

            decimal totalAmount = 0m;

            foreach (var item in normalizedItems)
            {
                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException("Receipt ingredient quantity must be greater than zero.");
                }

                if (item.Price <= 0)
                {
                    throw new InvalidOperationException("Receipt ingredient price must be greater than zero.");
                }

                var ingredient = EnsureIngredientExists(item.IngredientId);
                var receiptIngredient = new ReceiptIngredient
                {
                    ReceiptID = receipt.ReceiptID,
                    IngredientID = item.IngredientId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                _receiptRepository.AddIngredient(receiptIngredient);

                ingredient.StockQuantity += item.Quantity;
                _ingredientRepository.Update(ingredient);

                totalAmount += item.Quantity * item.Price;
            }

            _unitOfWork.SaveChanges();
            transaction.Commit();

            return new ProcurementReceiptResultDto
            {
                ReceiptId = receipt.ReceiptID,
                SupplierId = supplierId,
                StaffId = staffId,
                CreatedDate = createdAt,
                ItemCount = normalizedItems.Count,
                TotalAmount = totalAmount
            };
        }

        public decimal GetReceiptTotalValue(Receipt receipt)
        {
            if (receipt.ReceiptIngredients == null || receipt.ReceiptIngredients.Count == 0)
            {
                return 0m;
            }

            return receipt.ReceiptIngredients.Sum(x => x.Quantity * x.Price);
        }

        public IReadOnlyList<Ingredient> GetIngredients()
        {
            return _ingredientRepository.GetAll();
        }

        public IReadOnlyList<Staff> GetStaffs()
        {
            return _staffRepository.GetAllWithUser();
        }

        private static List<ProcurementReceiptItemInputDto> NormalizeItems(IEnumerable<ProcurementReceiptItemInputDto> items)
        {
            return items?
                .Where(x => x != null)
                .Select(x => new ProcurementReceiptItemInputDto
                {
                    IngredientId = x.IngredientId,
                    Quantity = x.Quantity,
                    Price = x.Price
                })
                .ToList()
                ?? [];
        }

        private Supplier EnsureSupplierExists(int supplierId)
        {
            var supplier = _supplierRepository.GetById(supplierId);

            if (supplier == null)
            {
                throw new InvalidOperationException("Supplier not found.");
            }

            return supplier;
        }

        private Staff EnsureStaffExists(int staffId)
        {
            var staff = _staffRepository.GetById(staffId);

            if (staff == null)
            {
                throw new InvalidOperationException("Staff not found.");
            }

            return staff;
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

        private void EnsureSupplierNameIsUnique(string name, int? ignoredSupplierId)
        {
            var existing = _supplierRepository.GetByName(name);

            if (existing == null)
            {
                return;
            }

            if (ignoredSupplierId.HasValue && existing.SupplierID == ignoredSupplierId.Value)
            {
                return;
            }

            throw new InvalidOperationException("Supplier name already exists.");
        }

        private static void ValidateSupplierValues(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Supplier name is required.");
            }
        }

        private static string? NormalizeOptionalValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
