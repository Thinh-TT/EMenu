using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IRoleRepository
    {
        IReadOnlyList<Role> GetAll();
        Role? GetByName(string roleName);
    }
}
