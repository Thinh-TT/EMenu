using EMenu.Application.Services;
using EMenu.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EMenu.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefault(x => x.UserName == username);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }
            if (!user.IsActive)
            {
                ViewBag.Error = "Account disabled";
                return View();
            }

            bool validPassword = false;

            if (!string.IsNullOrWhiteSpace(user.Password) &&
                user.Password.StartsWith("$2"))
            {
                validPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);
            }
            else if (user.Password == password)
            {
                validPassword = true;
                user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                _context.SaveChanges();
            }

            if (!validPassword)
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName)
                };

            foreach (var ur in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, ur.Role.RoleName));
            }

            var identity = new ClaimsIdentity(claims, "CookieAuth");

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal);

            var role = user.UserRoles.FirstOrDefault()?.Role?.RoleName;

            if (role == "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (role == "Staff")
            {
                return RedirectToAction("Index", "Table");
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");

            return RedirectToAction("Login");
        }
    }
}
