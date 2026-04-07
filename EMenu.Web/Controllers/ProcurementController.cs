using EMenu.Application.Abstractions.DTOs;
using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Web.Extensions;
using EMenu.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public class ProcurementController : Controller
    {
        private readonly ProcurementService _procurementService;
        private readonly ILogger<ProcurementController> _logger;

        public ProcurementController(
            ProcurementService procurementService,
            ILogger<ProcurementController> logger)
        {
            _procurementService = procurementService;
            _logger = logger;
        }

        public IActionResult Index(string? keyword)
        {
            return View(new SupplierIndexViewModel
            {
                Keyword = keyword,
                Suppliers = _procurementService.GetSuppliers(keyword)
            });
        }

        [HttpPost]
        public IActionResult CreateSupplier(string name, string? phone, string? email)
        {
            try
            {
                var supplier = _procurementService.CreateSupplier(name, phone, email);

                _logger.LogInformation(
                    "Supplier created by user {UserId} ({Username}) roles {Roles}: supplier {SupplierId} - {SupplierName}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    supplier.SupplierID,
                    supplier.Name);

                TempData["Success"] = "Supplier created successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateSupplier(int supplierId, string name, string? phone, string? email, string? keyword)
        {
            try
            {
                var supplier = _procurementService.UpdateSupplier(supplierId, name, phone, email);

                _logger.LogInformation(
                    "Supplier updated by user {UserId} ({Username}) roles {Roles}: supplier {SupplierId} - {SupplierName}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    supplier.SupplierID,
                    supplier.Name);

                TempData["Success"] = "Supplier updated successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { keyword });
        }

        [HttpPost]
        public IActionResult DeleteSupplier(int supplierId, string? keyword)
        {
            try
            {
                _procurementService.DeleteSupplier(supplierId);

                _logger.LogInformation(
                    "Supplier deleted by user {UserId} ({Username}) roles {Roles}: supplier {SupplierId}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    supplierId);

                TempData["Success"] = "Supplier deleted successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { keyword });
        }

        public IActionResult CreateReceipt()
        {
            return View(BuildCreateReceiptViewModel());
        }

        [HttpPost]
        public IActionResult CreateReceipt(
            int supplierId,
            int staffId,
            DateTime? createdDate,
            List<ProcurementReceiptLineInputViewModel>? items)
        {
            try
            {
                var result = _procurementService.CreateReceipt(
                    supplierId,
                    staffId,
                    ConvertToReceiptItems(items),
                    createdDate);

                _logger.LogInformation(
                    "Receipt created by user {UserId} ({Username}) roles {Roles}: receipt {ReceiptId}, supplier {SupplierId}, staff {StaffId}, total {Total}.",
                    User.GetAuditUserId(),
                    User.GetAuditUserName(),
                    User.GetAuditRoles(),
                    result.ReceiptId,
                    result.SupplierId,
                    result.StaffId,
                    result.TotalAmount);

                TempData["Success"] =
                    $"Receipt #{result.ReceiptId} created successfully. Total: {result.TotalAmount:N0}.";

                return RedirectToAction(
                    nameof(ReceiptHistory),
                    new
                    {
                        supplierId = result.SupplierId,
                        fromDate = result.CreatedDate.Date,
                        toDate = result.CreatedDate.Date
                    });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(CreateReceipt));
            }
        }

        public IActionResult ReceiptHistory(DateTime? fromDate, DateTime? toDate, int? supplierId)
        {
            try
            {
                var receipts = _procurementService.GetReceiptHistory(fromDate, toDate, supplierId);
                var rows = receipts
                    .Select(x => new ProcurementReceiptHistoryItemViewModel
                    {
                        ReceiptId = x.ReceiptID,
                        CreatedDate = x.CreatedDate,
                        SupplierId = x.SupplierID,
                        SupplierName = x.Supplier?.Name ?? $"Supplier {x.SupplierID}",
                        StaffId = x.StaffID,
                        StaffName = x.Staff?.StaffName ?? $"Staff {x.StaffID}",
                        ItemCount = x.ReceiptIngredients?.Count ?? 0,
                        TotalAmount = _procurementService.GetReceiptTotalValue(x),
                        Items = (x.ReceiptIngredients ?? [])
                            .Select(item => new ProcurementReceiptLineInputViewModel
                            {
                                IngredientId = item.IngredientID,
                                IngredientName = item.Ingredient?.Name ?? $"Ingredient {item.IngredientID}",
                                Quantity = item.Quantity,
                                Price = item.Price
                            })
                            .ToList()
                    })
                    .ToList();

                return View(new ProcurementReceiptHistoryViewModel
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    SupplierId = supplierId,
                    Suppliers = _procurementService.GetSuppliers(),
                    Receipts = rows,
                    TotalAmount = rows.Sum(x => x.TotalAmount)
                });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;

                return View(new ProcurementReceiptHistoryViewModel
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    SupplierId = supplierId,
                    Suppliers = _procurementService.GetSuppliers()
                });
            }
        }

        [HttpGet("/api/procurement/suppliers")]
        public IActionResult GetSuppliersApi(string? keyword)
        {
            var suppliers = _procurementService.GetSuppliers(keyword)
                .Select(x => new
                {
                    x.SupplierID,
                    x.Name,
                    x.Phone,
                    x.Email
                });

            return Ok(suppliers);
        }

        [HttpGet("/api/procurement/receipts")]
        public IActionResult GetReceiptsApi(DateTime? fromDate, DateTime? toDate, int? supplierId)
        {
            try
            {
                var receipts = _procurementService.GetReceiptHistory(fromDate, toDate, supplierId)
                    .Select(x => new
                    {
                        x.ReceiptID,
                        x.SupplierID,
                        supplierName = x.Supplier?.Name,
                        x.StaffID,
                        staffName = x.Staff?.StaffName,
                        x.CreatedDate,
                        itemCount = x.ReceiptIngredients?.Count ?? 0,
                        totalAmount = _procurementService.GetReceiptTotalValue(x)
                    });

                return Ok(receipts);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private ProcurementCreateReceiptViewModel BuildCreateReceiptViewModel()
        {
            var staffs = _procurementService.GetStaffs()
                .OrderBy(x => x.StaffName)
                .ToList();

            return new ProcurementCreateReceiptViewModel
            {
                SupplierId = _procurementService.GetSuppliers().FirstOrDefault()?.SupplierID,
                StaffId = staffs.FirstOrDefault()?.StaffID,
                CreatedDate = DateTime.Now,
                Suppliers = _procurementService.GetSuppliers(),
                Staffs = staffs,
                Ingredients = _procurementService.GetIngredients()
            };
        }

        private static List<ProcurementReceiptItemInputDto> ConvertToReceiptItems(
            List<ProcurementReceiptLineInputViewModel>? items)
        {
            if (items == null || items.Count == 0)
            {
                return [];
            }

            return items
                .Where(x => x != null)
                .Select(x => new ProcurementReceiptItemInputDto
                {
                    IngredientId = x.IngredientId,
                    Quantity = x.Quantity,
                    Price = x.Price
                })
                .ToList();
        }
    }
}
