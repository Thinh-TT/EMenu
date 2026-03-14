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
    public class StaffService
    {
        private readonly AppDbContext _context;

        public StaffService(AppDbContext context)
        {
            _context = context;
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

        public void Create(Staff staff, string username, string password)
        {
            var staffRole = _context.Roles
                .FirstOrDefault(x => x.RoleName == "Staff");

            if (staffRole == null)
                throw new Exception("Staff role not found");

            var user = new User
            {
                UserName = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
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
