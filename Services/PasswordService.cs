using Microsoft.AspNetCore.Identity;
using TaskManager.API.Interfaces;

namespace TaskManager.API.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<string> _hasher = new();

        public string HashPassword(string password)
        {
            return _hasher.HashPassword(null!, password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            var result = _hasher.VerifyHashedPassword(null!, hashedPassword, password);
            return result == PasswordVerificationResult.Success;
        }
    }
}