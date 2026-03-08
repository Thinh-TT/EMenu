using EMenu.Application.Services;
using EMenu.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly CategoryService _service;

        public CategoryController(CategoryService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var categories = _service.GetAll();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            _service.Create(category);
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var category = _service.GetById(id);
            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            _service.Update(category);
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
