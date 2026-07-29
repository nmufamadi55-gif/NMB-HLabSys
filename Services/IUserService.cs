using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;

namespace NMB_HLabSys.Services
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(string name, string surname, string email, string contactNumber, string role);
        Task<User> UpdateUserAsync(int userId, string name, string surname, string email, string contactNumber, bool isActive);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null);
        Task<string> GeneratePasswordAsync();
        Task<int> GetCurrentUserIdAsync(ClaimsPrincipal user);
        Task<string> GetCurrentUserNameAsync(ClaimsPrincipal user);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User> CreateUserAsync(string name, string surname, string email, string contactNumber, string role)
        {
            var user = new User
            {
                Name = name,
                Surname = surname,
                Email = email,
                ContactNumber = contactNumber,
                UserRole = role,
                IsActive = true,
                CreatedDate = DateTime.Now,
                PasswordHash = await GeneratePasswordAsync() // Temporary - should be handled by Identity
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User created: {email} with role {role}");
            return user;
        }

        public async Task<User> UpdateUserAsync(int userId, string name, string surname, string email, string contactNumber, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found");

            user.Name = name;
            user.Surname = surname;
            user.Email = email;
            user.ContactNumber = contactNumber;
            user.IsActive = isActive;
            user.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"User updated: {email} (ID: {userId})");
            return user;
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.Email == email);
            if (excludeUserId.HasValue)
                query = query.Where(u => u.UserID != excludeUserId.Value);

            return !await query.AnyAsync();
        }

        public async Task<string> GeneratePasswordAsync()
        {
            // This is a placeholder - use a proper password generation method
            return await Task.FromResult(Guid.NewGuid().ToString().Substring(0, 12));
        }

        public async Task<int> GetCurrentUserIdAsync(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;

            // If using ASP.NET Identity, the claim might be different
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                return dbUser?.UserID ?? 1;
            }

            return 1; // Default
        }

        public async Task<string> GetCurrentUserNameAsync(ClaimsPrincipal user)
        {
            var nameClaim = user.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(nameClaim))
                return nameClaim;

            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                return dbUser != null ? $"{dbUser.Name} {dbUser.Surname}" : "System User";
            }

            return "System User";
        }
    }
}