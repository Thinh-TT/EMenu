using EMenu.Application.Services;
using EMenu.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EMenu.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            var users = _service.GetAll();
            return View(users);
        }

        public IActionResult Create()
        {
            ViewBag.Roles = _service.GetRoles();
            return View();
        }

        [HttpPost]
        public IActionResult Create(User user, int roleId)
        {
            _service.Create(user, roleId);

            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            ViewBag.Roles = _service.GetRoles();

            var user = _service.GetById(id);

            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User user, int roleId)
        {
            _service.Update(user, roleId);

            return RedirectToAction("Index");
        }

        public IActionResult ToggleStatus(int id)
        {
            _service.ToggleStatus(id);

            return RedirectToAction("Index");
        }
    }
}
