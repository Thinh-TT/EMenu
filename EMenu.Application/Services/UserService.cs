using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Application.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;

        public UserService(AppDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public List<User> GetAll()
        {
            return _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ToList();
        }

        public User GetById(int id)
        {
            return _context.Users
                .Include(x => x.UserRoles)
                .FirstOrDefault(x => x.UserID == id);
        }

        public List<Role> GetRoles()
        {
            return _context.Roles.ToList();
        }

        public void Create(User user, int roleId, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                throw new ArgumentException("Username is required.");

            if (_context.Users.Any(x => x.UserName == user.UserName))
                throw new ArgumentException("Username already exists.");

            _passwordService.EnsureValidPassword(
                user.Password,
                confirmPassword,
                required: true);

            user.CreatedAt = DateTime.Now;
            user.IsActive = true;
            user.Password = _passwordService.HashPassword(user.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            var userRole = new UserRole
            {
                UserID = user.UserID,
                RoleID = roleId
            };

            _context.UserRoles.Add(userRole);
            _context.SaveChanges();
        }

        public void Update(User user, int roleId, string? confirmPassword)
        {
            var dbUser = _context.Users
                .Include(x => x.UserRoles)
                .FirstOrDefault(x => x.UserID == user.UserID);

            if (dbUser == null)
                throw new Exception("User not found");

            if (string.IsNullOrWhiteSpace(user.UserName))
                throw new ArgumentException("Username is required.");

            if (_context.Users.Any(x => x.UserID != user.UserID && x.UserName == user.UserName))
                throw new ArgumentException("Username already exists.");

            dbUser.UserName = user.UserName;

            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                _passwordService.EnsureValidPassword(
                    user.Password,
                    confirmPassword,
                    required: false);

                dbUser.Password = _passwordService.HashPassword(user.Password);
            }

            _context.Users.Update(dbUser);
            _context.SaveChanges();

            var userRole = _context.UserRoles
                .FirstOrDefault(x => x.UserID == dbUser.UserID);

            if (userRole != null)
            {
                userRole.RoleID = roleId;
                _context.SaveChanges();
            }
        }

        public void ToggleStatus(int id)
        {
            var user = _context.Users.Find(id);

            user.IsActive = !user.IsActive;

            _context.SaveChanges();
        }
    }
}
