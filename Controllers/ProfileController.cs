using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Profile/Index
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserRole = user.UserRole;
            ViewBag.UserName = $"{user.Name} {user.Surname}";
            ViewBag.UserEmail = user.Email;
            ViewBag.UserInitials = GetInitials($"{user.Name} {user.Surname}");
            ViewBag.UserGreeting = GetUserGreeting(user.UserRole, $"{user.Name} {user.Surname}");

            return View();
        }

        // POST: Profile/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string surname, string email, string phone)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            user.Name = name?.Trim() ?? user.Name;
            user.Surname = surname?.Trim() ?? user.Surname;
            user.ContactNumber = phone?.Trim() ?? user.ContactNumber;
            user.ModifiedDate = DateTime.Now;

            // Update session with new name
            HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check new passwords match
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match.";
                return RedirectBasedOnRole();
            }

            // Check password strength
            if (!IsValidPassword(newPassword))
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters, contain an uppercase letter, a number, and a special character.";
                return RedirectBasedOnRole();
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Verify current password
            var currentHash = PasswordHelper.HashPassword(currentPassword);
            if (user.PasswordHash != currentHash)
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectBasedOnRole();
            }

            // Update password
            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            user.IsPasswordChanged = true;
            user.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectBasedOnRole();
        }

        // Helper: Redirect based on role
        private IActionResult RedirectBasedOnRole()
        {
            var role = HttpContext.Session.GetString("UserRole");
            switch (role)
            {
                case "Patient":
                    return RedirectToAction("Profile", "Patient");
                case "Doctor":
                    return RedirectToAction("Dashboard", "Doctor");
                case "Manager":
                    return RedirectToAction("Index", "LaboratoryManager");
                case "Technician":
                    return RedirectToAction("Dashboard", "Technician");
                case "Admin":
                    return RedirectToAction("Dashboard", "Admin");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        private bool IsValidPassword(string password)
        {
            return !string.IsNullOrEmpty(password) &&
                   password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "U";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return parts[0][0].ToString() + parts[1][0].ToString();
            return name.Substring(0, 1).ToUpper();
        }

        private string GetUserGreeting(string role, string name)
        {
            switch (role)
            {
                case "Doctor": return $"👨‍⚕️ {name}";
                case "Technician": return $"🔬 {name}";
                case "Manager": return $"📊 {name}";
                case "Patient": return $"🩺 {name}";
                case "Admin": return $"⚙️ {name}";
                default: return name;
            }
        }
    }
}