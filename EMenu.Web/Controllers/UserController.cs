using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class UserController : Controller
    {
        private readonly UserService _service;
        private readonly PasswordService _passwordService;

        public UserController(UserService service, PasswordService passwordService)
        {
            _service = service;
            _passwordService = passwordService;
        }

        public IActionResult Index()
        {
            var users = _service.GetAll();
            return View(users);
        }

        public IActionResult Create()
        {
            PopulateCreateView();
            return View();
        }

        [HttpPost]
        public IActionResult Create(User user, int roleId, string? confirmPassword)
        {
            try
            {
                _service.Create(user, roleId, confirmPassword);
                return RedirectToAction("Index");
            }
            catch (ArgumentException ex)
            {
                PopulateCreateView();
                ViewBag.Error = ex.Message;
                return View(user);
            }
        }

        public IActionResult Edit(int id)
        {
            PopulateEditView();

            var user = _service.GetById(id);

            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User user, int roleId, string? confirmPassword)
        {
            try
            {
                _service.Update(user, roleId, confirmPassword);
                return RedirectToAction("Index");
            }
            catch (ArgumentException ex)
            {
                PopulateEditView();
                ViewBag.Error = ex.Message;
                return View(user);
            }
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            _service.ToggleStatus(id);

            return RedirectToAction("Index");
        }

        private void PopulateCreateView()
        {
            ViewBag.Roles = _service.GetRoles();
            ViewBag.PasswordPolicy = _passwordService.PolicyDescription;
        }

        private void PopulateEditView()
        {
            ViewBag.Roles = _service.GetRoles();
            ViewBag.PasswordPolicy = _passwordService.PolicyDescription;
        }
    }
}
