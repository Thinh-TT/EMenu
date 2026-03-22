using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMenu.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<User> GetAllWithRoles()
        {
            return _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ToList();
        }

        public User? GetById(int userId)
        {
            return _context.Users.FirstOrDefault(x => x.UserID == userId);
        }

        public User? GetByIdWithRoles(int userId)
        {
            return _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefault(x => x.UserID == userId);
        }

        public User? GetByUsernameWithRoles(string username)
        {
            return _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefault(x => x.UserName == username);
        }

        public bool ExistsByUsername(string username)
        {
            return _context.Users.Any(x => x.UserName == username);
        }

        public bool ExistsByUsernameExceptId(int userId, string username)
        {
            return _context.Users.Any(x => x.UserID != userId && x.UserName == username);
        }

        public IReadOnlyList<User> GetLegacyPasswordUsers()
        {
            return _context.Users
                .Where(x => !string.IsNullOrWhiteSpace(x.Password) && !x.Password.StartsWith("$2"))
                .ToList();
        }

        public UserRole? GetUserRoleByUserId(int userId)
        {
            return _context.UserRoles.FirstOrDefault(x => x.UserID == userId);
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
        }

        public void AddUserRole(UserRole userRole)
        {
            _context.UserRoles.Add(userRole);
        }
    }
}
