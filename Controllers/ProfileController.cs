using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public ProfileController(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.UserRole = user.UserRole;
            ViewBag.Name = user.Name;
            ViewBag.Surname = user.Surname;
            ViewBag.Email = user.Email;
            ViewBag.ContactNumber = user.ContactNumber;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string surname, string email, string phone)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login", "Account");

            // Check if new email is taken by someone else
            var emailTaken = await _context.Users.AnyAsync(u => u.Email == email.ToLower() && u.UserID != userId.Value);
            if (emailTaken)
            {
                TempData["ErrorMessage"] = "That email address is already in use by another account.";
                return RedirectToAction("Index");
            }

            user.Name = name?.Trim() ?? user.Name;
            user.Surname = surname?.Trim() ?? user.Surname;
            user.Email = email?.Trim().ToLower() ?? user.Email;
            user.ContactNumber = phone?.Trim() ?? user.ContactNumber;
            user.ModifiedDate = DateTime.Now;

            // Update session info
            HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}");
            HttpContext.Session.SetString("UserEmail", user.Email);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match.";
                return RedirectToAction("Index");
            }

            if (!IsValidPassword(newPassword))
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters, contain an uppercase letter, a number, and a special character.";
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("Index");
            }

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.IsPasswordChanged = true;
            user.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Index");
        }

        private bool IsValidPassword(string password)
        {
            return !string.IsNullOrEmpty(password) &&
                   password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }
}