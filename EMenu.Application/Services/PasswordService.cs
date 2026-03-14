using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace EMenu.Application.Services
{
    public class PasswordService
    {
        private const int MinimumLength = 8;

        public string PolicyDescription =>
            "Password must be at least 8 characters long and include at least 1 letter and 1 number.";

        public bool IsHashed(string? password)
        {
            return !string.IsNullOrWhiteSpace(password) &&
                password.StartsWith("$2");
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string inputPassword, string? storedPassword)
        {
            if (string.IsNullOrWhiteSpace(storedPassword))
                return false;

            if (IsHashed(storedPassword))
                return BCrypt.Net.BCrypt.Verify(inputPassword, storedPassword);

            return storedPassword == inputPassword;
        }

        public void EnsureValidPassword(
            string? password,
            string? confirmPassword,
            bool required)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                if (required)
                    throw new ArgumentException("Password is required.");

                return;
            }

            if (password != confirmPassword)
                throw new ArgumentException("Password confirmation does not match.");

            if (password.Length < MinimumLength)
                throw new ArgumentException($"Password must be at least {MinimumLength} characters.");

            if (!Regex.IsMatch(password, "[A-Za-z]"))
                throw new ArgumentException("Password must include at least 1 letter.");

            if (!Regex.IsMatch(password, "[0-9]"))
                throw new ArgumentException("Password must include at least 1 number.");
        }

        public int MigrateLegacyPasswords(AppDbContext context)
        {
            var legacyUsers = context.Users
                .Where(x => !string.IsNullOrWhiteSpace(x.Password) && !x.Password.StartsWith("$2"))
                .ToList();

            if (!legacyUsers.Any())
                return 0;

            foreach (var user in legacyUsers)
            {
                user.Password = HashPassword(user.Password);
            }

            context.SaveChanges();

            return legacyUsers.Count;
        }
    }
}
