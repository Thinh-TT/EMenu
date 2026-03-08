using EMenu.Application.Services;
using EMenu.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class ComboController : Controller
    {
        private readonly ComboService _service;

        public ComboController(ComboService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var combos = _service.GetCombos();

            return View(combos);
        }

        public IActionResult Create()
        {
            ViewBag.Products = _service.GetProducts();

            return View();
        }

        [HttpPost]
        public IActionResult Create(Product combo, List<int> productIds)
        {
            combo.ProductType = Domain.Enums.ProductType.Combo;

            _service.CreateCombo(combo, productIds);

            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            ViewBag.Products = _service.GetSingleProducts();

            ViewBag.ComboItems = _service.GetComboItemIds(id);

            var combo = _service.GetCombos()
                .First(x => x.ProductID == id);

            return View(combo);
        }

        [HttpPost]
        public IActionResult Edit(int comboId, List<int> productIds)
        {
            _service.UpdateCombo(comboId, productIds);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetItems(int comboId)
        {
            var items = _service.GetComboItems(comboId);

            return Json(items);
        }

    }
}
