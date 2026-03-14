using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class ProductController : Controller
    {
        private readonly ProductService _service;
        private readonly IWebHostEnvironment _env;

        public ProductController(ProductService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        public IActionResult Index()
        {
            var products = _service.GetAll();
            return View(products);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = _service.GetCategories();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Product product, IFormFile ImageFile)
        {
            if (ImageFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "images/products");
                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                product.Image = fileName;
            }

            _service.Create(product);

            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            ViewBag.Categories = _service.GetCategories();

            var product = _service.GetById(id);

            return View(product);
        }

        [HttpPost]
        public IActionResult Edit(Product product, IFormFile ImageFile)
        {
            var dbProduct = _service.GetById(product.ProductID);

            dbProduct.ProductName = product.ProductName;
            dbProduct.Price = product.Price;
            dbProduct.CategoryID = product.CategoryID;
            dbProduct.Description = product.Description;
            dbProduct.IsAvailable = product.IsAvailable;

            if (ImageFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "images/products");

                if (!string.IsNullOrEmpty(dbProduct.Image))
                {
                    string oldPath = Path.Combine(folder, dbProduct.Image);

                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                string fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                dbProduct.Image = fileName;
            }

            _service.Update(dbProduct);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
