using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Domain.Entities;
using EMenu.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    public class InventoryController : Controller
    {
        private readonly InventoryService _inventoryService;

        public InventoryController(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public IActionResult Index(string? keyword, bool lowStockOnly = false, string sortBy = "name", bool descending = false)
        {
            var ingredients = _inventoryService.GetAllIngredients().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalized = keyword.Trim();
                ingredients = ingredients.Where(x => x.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase));
            }

            if (lowStockOnly)
            {
                ingredients = ingredients.Where(x => x.StockQuantity <= x.MinStock);
            }

            ingredients = SortIngredients(ingredients, sortBy, descending);

            var vm = new InventoryIndexViewModel
            {
                Keyword = keyword,
                LowStockOnly = lowStockOnly,
                SortBy = sortBy,
                Descending = descending,
                LowStockCount = _inventoryService.GetLowStockIngredients().Count,
                Ingredients = ingredients.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult CreateIngredient(string name, string unit, decimal stockQuantity, decimal minStock)
        {
            try
            {
                _inventoryService.CreateIngredient(name, unit, stockQuantity, minStock);
                TempData["Success"] = "Ingredient created successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateIngredient(int ingredientId, string name, string unit, decimal stockQuantity, decimal minStock)
        {
            try
            {
                _inventoryService.UpdateIngredient(ingredientId, name, unit, stockQuantity, minStock);
                TempData["Success"] = "Ingredient updated successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult DeleteIngredient(int ingredientId)
        {
            try
            {
                _inventoryService.DeleteIngredient(ingredientId);
                TempData["Success"] = "Ingredient deleted successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult AddStock(int ingredientId, decimal quantity)
        {
            try
            {
                _inventoryService.AddStock(ingredientId, quantity);
                TempData["Success"] = "Stock updated successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Mapping(int? productId)
        {
            var products = _inventoryService
                .GetAllProducts()
                .OrderBy(x => x.ProductName)
                .ToList();

            var ingredients = _inventoryService.GetAllIngredients();

            if (!products.Any())
            {
                return View(new InventoryMappingViewModel
                {
                    Products = products,
                    Ingredients = ingredients
                });
            }

            var selectedProductId = productId ?? products.First().ProductID;
            var mappings = _inventoryService.GetIngredientsByProduct(selectedProductId);

            return View(new InventoryMappingViewModel
            {
                SelectedProductId = selectedProductId,
                Products = products,
                Ingredients = ingredients,
                Mappings = mappings
            });
        }

        [HttpPost]
        public IActionResult UpsertMapping(int productId, int ingredientId, decimal quantity)
        {
            try
            {
                _inventoryService.UpsertIngredientProduct(productId, ingredientId, quantity);
                TempData["Success"] = "Ingredient mapping saved successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Mapping), new { productId });
        }

        [HttpPost]
        public IActionResult RemoveMapping(int id, int productId)
        {
            _inventoryService.RemoveIngredientProduct(id);
            TempData["Success"] = "Ingredient mapping removed.";

            return RedirectToAction(nameof(Mapping), new { productId });
        }

        [HttpGet("/api/inventory/low-stock")]
        public IActionResult GetLowStock()
        {
            var lowStock = _inventoryService.GetLowStockIngredients()
                .Select(x => new
                {
                    x.IngredientID,
                    x.Name,
                    x.Unit,
                    x.StockQuantity,
                    x.MinStock
                });

            return Ok(lowStock);
        }

        [HttpGet("/api/inventory/product/{productId:int}/ingredients")]
        public IActionResult GetIngredientsByProductApi(int productId)
        {
            try
            {
                var items = _inventoryService.GetIngredientsByProduct(productId)
                    .Select(x => new
                    {
                        x.Id,
                        x.ProductID,
                        productName = x.Product?.ProductName,
                        x.IngredientID,
                        ingredientName = x.Ingredient?.Name,
                        ingredientUnit = x.Ingredient?.Unit,
                        x.Quantity
                    });

                return Ok(items);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("/api/inventory/ingredient/{ingredientId:int}/products")]
        public IActionResult GetProductsByIngredientApi(int ingredientId)
        {
            try
            {
                var items = _inventoryService.GetProductsByIngredient(ingredientId)
                    .Select(x => new
                    {
                        x.Id,
                        x.IngredientID,
                        ingredientName = x.Ingredient?.Name,
                        x.ProductID,
                        productName = x.Product?.ProductName,
                        x.Quantity
                    });

                return Ok(items);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static IEnumerable<Ingredient> SortIngredients(
            IEnumerable<Ingredient> ingredients,
            string sortBy,
            bool descending)
        {
            var normalizedSortBy = (sortBy ?? "name").Trim().ToLowerInvariant();

            Func<Ingredient, object> keySelector = normalizedSortBy switch
            {
                "stock" => x => x.StockQuantity,
                "min" => x => x.MinStock,
                "gap" => x => x.StockQuantity - x.MinStock,
                _ => x => x.Name
            };

            return descending
                ? ingredients.OrderByDescending(keySelector)
                : ingredients.OrderBy(keySelector);
        }
    }
}
