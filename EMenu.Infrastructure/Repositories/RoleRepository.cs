using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Infrastructure.Data;

namespace EMenu.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<Role> GetAll()
        {
            return _context.Roles.ToList();
        }

        public Role? GetByName(string roleName)
        {
            return _context.Roles.FirstOrDefault(x => x.RoleName == roleName);
        }
    }
}
