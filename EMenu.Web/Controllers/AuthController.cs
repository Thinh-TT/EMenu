using EMenu.Application.Services;
using EMenu.Domain.Constants;
using EMenu.Infrastructure.Data;
using EMenu.Web.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EMenu.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            PasswordService passwordService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefault(x => x.UserName == username);

            if (user == null)
            {
                _logger.LogWarning(
                    "Login failed for username {Username}: user not found.",
                    username);
                ViewBag.Error = "Invalid username or password";
                return View();
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Login blocked for user {UserId} ({Username}): account disabled.",
                    user.UserID,
                    user.UserName);
                ViewBag.Error = "Account disabled";
                return View();
            }

            bool validPassword = _passwordService.VerifyPassword(password, user.Password);

            if (validPassword && !_passwordService.IsHashed(user.Password))
            {
                user.Password = _passwordService.HashPassword(password);
                _context.SaveChanges();
            }

            if (!validPassword)
            {
                _logger.LogWarning(
                    "Login failed for user {UserId} ({Username}): invalid password.",
                    user.UserID,
                    user.UserName);
                ViewBag.Error = "Invalid username or password";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            foreach (var roleName in user.UserRoles
                .Select(x => x.Role?.RoleName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()!)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName!));
            }

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal);

            var roles = user.UserRoles
                .Select(x => x.Role?.RoleName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Login succeeded for user {UserId} ({Username}) with roles {Roles}.",
                user.UserID,
                user.UserName,
                string.Join(",", roles));

            if (roles.Contains(AppRoles.Admin))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (roles.Contains(AppRoles.Staff))
            {
                return RedirectToAction("Index", "Table");
            }

            if (roles.Contains(AppRoles.Kitchen))
            {
                return RedirectToAction("Index", "Kitchen");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation(
                "Logout for user {UserId} ({Username}) with roles {Roles}.",
                User.GetAuditUserId(),
                User.GetAuditUserName(),
                User.GetAuditRoles());

            await HttpContext.SignOutAsync("CookieAuth");

            return RedirectToAction("Login");
        }
    }
}
