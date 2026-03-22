using EMenu.Domain.Entities;

namespace EMenu.Application.Abstractions.Repositories
{
    public interface IUserRepository
    {
        IReadOnlyList<User> GetAllWithRoles();
        User? GetById(int userId);
        User? GetByIdWithRoles(int userId);
        User? GetByUsernameWithRoles(string username);
        bool ExistsByUsername(string username);
        bool ExistsByUsernameExceptId(int userId, string username);
        IReadOnlyList<User> GetLegacyPasswordUsers();
        UserRole? GetUserRoleByUserId(int userId);
        void Add(User user);
        void Update(User user);
        void AddUserRole(UserRole userRole);
    }
}
