// File: ProfileController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    /// <summary>
    /// Shared profile controller.
    /// - Doctors get the rich, role-specific profile view (~/Views/Doctor/Profile.cshtml)
    ///   with doctor-specific ViewBag data (HPCSA, practice, verification, etc.).
    /// - All other roles keep the existing generic profile view.
    /// </summary>
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICurrentUserService _currentUser;

        public ProfileController(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            ICurrentUserService currentUser)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _currentUser = currentUser;
        }

        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════
        private int? CurrentUserId =>
            _currentUser.UserId ?? HttpContext.Session.GetInt32("UserID");

        private string CurrentUserRole =>
            _currentUser.UserRole ?? HttpContext.Session.GetString("UserRole") ?? "User";

        private IActionResult RedirectToProfile()
            => RedirectToAction("Index", "Profile");

        // ═══════════════════════════════════════════════════════════
        // GET: /Profile/Index
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            var role = string.IsNullOrWhiteSpace(user.UserRole)
                ? CurrentUserRole
                : user.UserRole;

            // ── Common fields used by BOTH views ──
            PopulateCommonViewBag(user, role);

            // ── Doctors: full rich profile (original version 1 behaviour) ──
            if (string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                await PopulateDoctorViewBagAsync(user.UserID);
                return View("~/Views/Doctor/Profile.cshtml");
            }

            // ── Everyone else: existing generic profile ──
            ViewBag.UserRole = user.UserRole;
            ViewBag.Name = user.Name;
            ViewBag.Surname = user.Surname;
            ViewBag.Email = user.Email;
            ViewBag.ContactNumber = user.ContactNumber;

            return View(); // ~/Views/Profile/Index.cshtml
        }

        // ═══════════════════════════════════════════════════════════
        // POST: /Profile/UpdateProfile
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string name, string surname, string email, string phone)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Uniqueness check for email (only if changing)
            if (!string.IsNullOrWhiteSpace(email) &&
                !email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var taken = await _context.Users.AnyAsync(u =>
                    u.Email.ToLower() == email.ToLower() &&
                    u.UserID != user.UserID);

                if (taken)
                {
                    TempData["ErrorMessage"] = "That email address is already in use by another account.";
                    return RedirectToProfile();
                }
            }

            if (!string.IsNullOrWhiteSpace(name)) user.Name = name.Trim();
            if (!string.IsNullOrWhiteSpace(surname)) user.Surname = surname.Trim();
            if (!string.IsNullOrWhiteSpace(email)) user.Email = email.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(phone)) user.ContactNumber = phone.Trim();

            user.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // Keep session in sync
            HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}".Trim());
            HttpContext.Session.SetString("UserEmail", user.Email);

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToProfile();
        }

        // ═══════════════════════════════════════════════════════════
        // POST: /Profile/ChangePassword
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["ErrorMessage"] = "Current password is required.";
                return RedirectToProfile();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "New password must be at least 8 characters.";
                return RedirectToProfile();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToProfile();
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToProfile();
            }

            if (_passwordHasher.VerifyPassword(newPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "New password must be different from the current one.";
                return RedirectToProfile();
            }

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.IsPasswordChanged = true;
            user.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToProfile();
        }

        // ═══════════════════════════════════════════════════════════
        // DOCTOR-SPECIFIC VIEWBAG (from version 1)
        // ═══════════════════════════════════════════════════════════
        private async Task PopulateDoctorViewBagAsync(int userId)
        {
            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserID == userId);

            if (doctor != null)
            {
                ViewBag.DoctorID = doctor.DoctorID;
                ViewBag.HPCSANumber = doctor.HPCSANumber;
                ViewBag.PracticeName = doctor.PracticeName;
                ViewBag.PracticeAddress = doctor.PracticeAddress;
                ViewBag.Specialization = doctor.Specialization;
                ViewBag.IsVerified = doctor.IsVerified;
                ViewBag.RegisteredDate = doctor.RegisteredDate;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // COMMON VIEWBAG (fields the doctor view relies on)
        // ═══════════════════════════════════════════════════════════
        private void PopulateCommonViewBag(NMB_HLabSys.Models.User user, string role)
        {
            ViewBag.UserRole = role;
            ViewBag.UserEmail = user.Email;
            ViewBag.UserName = $"{user.Name} {user.Surname}".Trim();
            ViewBag.UserInitials = GetInitials(user.Name, user.Surname);
            ViewBag.UserGreeting = GetGreeting(role, user.Name);
            ViewBag.ContactNumber = user.ContactNumber;
            ViewBag.IsActive = user.IsActive;
            ViewBag.IsPasswordChanged = user.IsPasswordChanged;
            ViewBag.CreatedDate = user.CreatedDate;
            ViewBag.LastModified = user.ModifiedDate;
            ViewBag.RoleSubtitle = role switch
            {
                "Doctor" => "Doctor Portal",
                "Technician" => "Technician Portal",
                "Patient" => "Patient Portal",
                "Manager" => "Manager Portal",
                "Admin" => "Admin Portal",
                _ => "Portal"
            };
        }

        private static string GetInitials(string name, string surname)
        {
            var n = string.IsNullOrWhiteSpace(name) ? "?" : name[0].ToString().ToUpper();
            var s = string.IsNullOrWhiteSpace(surname) ? "" : surname[0].ToString().ToUpper();
            return (n + s).Trim();
        }

        private static string GetGreeting(string role, string name)
        {
            return role switch
            {
                "Doctor" => $"Dr. {name}",
                _ => name
            };
        }
    }
}