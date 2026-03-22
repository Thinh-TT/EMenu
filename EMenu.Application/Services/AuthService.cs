using EMenu.Application.Abstractions.Persistence;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;

namespace EMenu.Application.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordService _passwordService;

        public AuthService(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            PasswordService passwordService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
        }

        public User? Login(string username, string password)
        {
            var user = _userRepository.GetByUsernameWithRoles(username);

            if (user == null || !user.IsActive)
                return null;

            var validPassword = _passwordService.VerifyPassword(password, user.Password);

            if (!validPassword)
                return null;

            if (!_passwordService.IsHashed(user.Password))
            {
                user.Password = _passwordService.HashPassword(password);
                _unitOfWork.SaveChanges();
            }

            return user;
        }
    }
}
