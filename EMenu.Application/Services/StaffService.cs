using EMenu.Domain.Constants;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Application.Services
{
    public class StaffService
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;

        public StaffService(AppDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public List<Staff> GetAll()
        {
            return _context.Staffs
                .Include(x => x.User)
                .ToList();
        }

        public Staff GetById(int id)
        {
            return _context.Staffs
                .Include(x => x.User)
                .FirstOrDefault(x => x.StaffID == id);
        }

        public void Create(Staff staff, string username, string password, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required.");

            if (_context.Users.Any(x => x.UserName == username))
                throw new ArgumentException("Username already exists.");

            _passwordService.EnsureValidPassword(
                password,
                confirmPassword,
                required: true);

            var staffRole = _context.Roles
                .FirstOrDefault(x => x.RoleName == AppRoles.Staff);

            if (staffRole == null)
                throw new Exception("Staff role not found");

            var user = new User
            {
                UserName = username,
                Password = _passwordService.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            _context.UserRoles.Add(new UserRole
            {
                UserID = user.UserID,
                RoleID = staffRole.RoleID
            });

            _context.SaveChanges();

            staff.UserID = user.UserID;

            _context.Staffs.Add(staff);
            _context.SaveChanges();
        }

        public void Update(Staff staff)
        {
            _context.Staffs.Update(staff);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var staff = _context.Staffs.Find(id);

            if (staff != null)
            {
                _context.Staffs.Remove(staff);
                _context.SaveChanges();
            }
        }
    }
}
