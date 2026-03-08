using EMenu.Application.Services;
using EMenu.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class StaffController : Controller
    {
        private readonly StaffService _service;

        public StaffController(StaffService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var staffs = _service.GetAll();
            return View(staffs);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Staff staff, string username, string password)
        {
            _service.Create(staff, username, password);

            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var staff = _service.GetById(id);
            return View(staff);
        }

        [HttpPost]
        public IActionResult Edit(Staff staff)
        {
            _service.Update(staff);
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
