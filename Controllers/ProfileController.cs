// File: ProfileController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Filters;
using NMB_HLabSys.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    [RequireSession]
    [NoCache]
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

        /// <summary>
        /// Redirect to profile page based on role.
        /// Patients → Patient/Profile
        /// Doctors → Doctor/Profile (via Profile/Index which routes to doctor view)
        /// Everyone else → Profile/Index
        /// </summary>
        private IActionResult RedirectToProfileTab(string tab)
        {
            var role = CurrentUserRole;

            if (string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
            {
                // Patient has its own profile page — preserve the ?tab anchor if needed
                return RedirectToAction("Profile", "Patient");
            }

            // Doctor, Technician, Manager, Admin → shared profile page
            return RedirectToAction("Index", "Profile", new { tab });
        }
        private static bool IsPasswordComplex(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            return password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(ch => !char.IsLetterOrDigit(ch));
        }
        // ═══════════════════════════════════════════════════════════
        // GET: /Profile/Index
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string? tab)
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

            // ── Patients get their own dedicated page ──
            if (string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Profile", "Patient");
            }

            PopulateCommonViewBag(user, role);

            ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab;

            if (string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                await PopulateDoctorViewBagAsync(user.UserID);
                return View("~/Views/Doctor/Profile.cshtml");
            }

            ViewBag.UserRole = user.UserRole;
            ViewBag.Name = user.Name;
            ViewBag.Surname = user.Surname;
            ViewBag.Email = user.Email;
            ViewBag.ContactNumber = user.ContactNumber;

            return View();
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
                    return RedirectToProfileTab("overview");
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
            return RedirectToProfileTab("overview");
        }

        // ═══════════════════════════════════════════════════════════
        // POST: /Profile/ChangePassword
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> ChangePassword(
            string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["ErrorMessage"] = "Current password is required.";
                return RedirectToProfileTab("security");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "New password must be at least 8 characters.";
                return RedirectToProfileTab("security");
            }

            if (!IsPasswordComplex(newPassword))
            {
                TempData["ErrorMessage"] = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.";
                return RedirectToProfileTab("security");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToProfileTab("security");
            }

            if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToProfileTab("security");
            }

            if (_passwordHasher.VerifyPassword(newPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "New password must be different from the current one.";
                return RedirectToProfileTab("security");
            }

            // ── Success ──
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.IsPasswordChanged = true;
            user.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("ForcePasswordChange");
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToProfileTab("security");
        }

        // ═══════════════════════════════════════════════════════════
        // DOCTOR-SPECIFIC VIEWBAG
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
        // COMMON VIEWBAG
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