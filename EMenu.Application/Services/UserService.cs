using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMenu.Application.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
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

        public void Create(User user, int roleId)
        {
            user.CreatedAt = DateTime.Now;

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

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

        public void Update(User user, int roleId)
        {
            if (!string.IsNullOrEmpty(user.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            }

            _context.Users.Update(user);
            _context.SaveChanges();

            var userRole = _context.UserRoles
                .FirstOrDefault(x => x.UserID == user.UserID);

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
