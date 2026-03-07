using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;

namespace EMenu.Application.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public User Login(string username, string password)
        {
            return _context.Users
                .FirstOrDefault(x =>
                    x.UserName == username &&
                    x.Password == password &&
                    x.IsActive);
        }
    }
}
