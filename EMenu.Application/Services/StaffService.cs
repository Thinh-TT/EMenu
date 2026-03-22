using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Constants;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class StaffService
    {
        private readonly IStaffRepository _staffRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordService _passwordService;

        public StaffService(
            IStaffRepository staffRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            PasswordService passwordService)
        {
            _staffRepository = staffRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
        }

        public List<Staff> GetAll()
        {
            return _staffRepository.GetAllWithUser().ToList();
        }

        public Staff GetById(int id)
        {
            return _staffRepository.GetByIdWithUser(id);
        }

        public void Create(Staff staff, string username, string password, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required.");

            if (_userRepository.ExistsByUsername(username))
                throw new ArgumentException("Username already exists.");

            _passwordService.EnsureValidPassword(
                password,
                confirmPassword,
                required: true);

            var staffRole = _roleRepository.GetByName(AppRoles.Staff);

            if (staffRole == null)
                throw new Exception("Staff role not found");

            using var transaction = _unitOfWork.BeginTransaction();

            var user = new User
            {
                UserName = username,
                Password = _passwordService.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _userRepository.Add(user);

            _userRepository.AddUserRole(new UserRole
            {
                User = user,
                RoleID = staffRole.RoleID
            });

            staff.User = user;

            _staffRepository.Add(staff);
            _unitOfWork.SaveChanges();

            transaction.Commit();
        }

        public void Update(Staff staff)
        {
            _staffRepository.Update(staff);
            _unitOfWork.SaveChanges();
        }

        public void Delete(int id)
        {
            var staff = _staffRepository.GetById(id);

            if (staff != null)
            {
                _staffRepository.Remove(staff);
                _unitOfWork.SaveChanges();
            }
        }
    }
}
