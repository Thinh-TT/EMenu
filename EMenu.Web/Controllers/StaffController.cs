using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class StaffController : Controller
    {
        private readonly StaffService _service;
        private readonly PasswordService _passwordService;

        public StaffController(StaffService service, PasswordService passwordService)
        {
            _service = service;
            _passwordService = passwordService;
        }

        public IActionResult Index()
        {
            var staffs = _service.GetAll();
            return View(staffs);
        }

        public IActionResult Create()
        {
            ViewBag.PasswordPolicy = _passwordService.PolicyDescription;
            return View();
        }

        [HttpPost]
        public IActionResult Create(Staff staff, string username, string password, string? confirmPassword)
        {
            try
            {
                _service.Create(staff, username, password, confirmPassword);
                return RedirectToAction("Index");
            }
            catch (ArgumentException ex)
            {
                ViewBag.PasswordPolicy = _passwordService.PolicyDescription;
                ViewBag.Error = ex.Message;
                return View(staff);
            }
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

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
