using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordService _passwordService;

        public UserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            PasswordService passwordService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
        }

        public List<User> GetAll()
        {
            return _userRepository.GetAllWithRoles().ToList();
        }

        public User GetById(int id)
        {
            return _userRepository.GetByIdWithRoles(id);
        }

        public List<Role> GetRoles()
        {
            return _roleRepository.GetAll().ToList();
        }

        public void Create(User user, int roleId, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                throw new ArgumentException("Username is required.");

            if (_userRepository.ExistsByUsername(user.UserName))
                throw new ArgumentException("Username already exists.");

            _passwordService.EnsureValidPassword(
                user.Password,
                confirmPassword,
                required: true);

            user.CreatedAt = DateTime.Now;
            user.IsActive = true;
            user.Password = _passwordService.HashPassword(user.Password);

            using var transaction = _unitOfWork.BeginTransaction();

            _userRepository.Add(user);

            var userRole = new UserRole
            {
                User = user,
                RoleID = roleId
            };

            _userRepository.AddUserRole(userRole);
            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        public void Update(User user, int roleId, string? confirmPassword)
        {
            var dbUser = _userRepository.GetByIdWithRoles(user.UserID);

            if (dbUser == null)
                throw new Exception("User not found");

            if (string.IsNullOrWhiteSpace(user.UserName))
                throw new ArgumentException("Username is required.");

            if (_userRepository.ExistsByUsernameExceptId(user.UserID, user.UserName))
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

            using var transaction = _unitOfWork.BeginTransaction();

            _userRepository.Update(dbUser);

            var userRole = _userRepository.GetUserRoleByUserId(dbUser.UserID);

            if (userRole != null)
            {
                userRole.RoleID = roleId;
            }
            else
            {
                _userRepository.AddUserRole(new UserRole
                {
                    UserID = dbUser.UserID,
                    RoleID = roleId
                });
            }

            _unitOfWork.SaveChanges();
            transaction.Commit();
        }

        public void ToggleStatus(int id)
        {
            var user = _userRepository.GetById(id);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            user.IsActive = !user.IsActive;

            _unitOfWork.SaveChanges();
        }
    }
}
