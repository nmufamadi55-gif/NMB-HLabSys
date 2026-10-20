using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Services;
using NMB_HLabSys.ViewModels.AdminViewModels;
using NMB_HLabSys.ViewModels.SharedViewModels;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IEmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        // ============================================
        // LOGIN
        // ============================================
        public IActionResult Login()
        {
            HttpContext.Session.Clear();
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email.ToLower() && u.IsActive);

                if (user == null || !_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                if (_passwordHasher.NeedsRehash(user.PasswordHash))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(model.Password);
                    user.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("UserRole", user.UserRole);
                HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}");
                HttpContext.Session.SetString("UserEmail", user.Email);

                if (user.UserRole == "Patient")
                    return RedirectToAction("Dashboard", "Patient");
                else if (user.UserRole == "Admin")
                    return RedirectToAction("Dashboard", "Admin");
                else if (user.UserRole == "Doctor")
                    return RedirectToAction("Dashboard", "Doctor");       // ✅ Fixed
                else if (user.UserRole == "Technician")
                    return RedirectToAction("Dashboard", "Technician");   // ✅ Fixed
                else if (user.UserRole == "Manager")
                    return RedirectToAction("Index", "LaboratoryManager");

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }

        // ============================================
        // FORGOT PASSWORD — step 1
        // ============================================
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            const string neutralMessage =
                "If an account with that email exists, a password reset link has been sent. " +
                "Please check your inbox (and spam folder). The link expires in 30 minutes.";

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Please enter your email address.";
                return View();
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower() && u.IsActive);

                if (user != null)
                {
                    // Invalidate previous unused tokens
                    var oldTokens = _context.PasswordResetTokens
                        .Where(t => t.UserID == user.UserID && !t.IsUsed);
                    _context.PasswordResetTokens.RemoveRange(oldTokens);

                    // Generate secure token
                    var token = GenerateSecureToken();

                    _context.PasswordResetTokens.Add(new PasswordResetToken
                    {
                        UserID = user.UserID,
                        Token = token,
                        ExpiresAt = DateTime.Now.AddMinutes(30),
                        IsUsed = false,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    // BaseUrl derived from current request — no config needed
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var resetLink = $"{baseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";

                    // Send email
                    await _emailService.SendPasswordResetEmailAsync(
                        user.Email,
                        $"{user.Name} {user.Surname}",
                        resetLink);
                }

                TempData["SuccessMessage"] = neutralMessage;
                return RedirectToAction("ForgotPassword");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"EMAIL FAILED: {ex.Message} | INNER: {ex.InnerException?.Message}";
                return View();
            }
        }

        // ============================================
        // RESET PASSWORD — step 2 (user clicked email link)
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid reset link.";
                return RedirectToAction("ForgotPassword");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower() && u.IsActive);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid reset link.";
                return RedirectToAction("ForgotPassword");
            }

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.UserID == user.UserID &&
                    t.Token == token &&
                    !t.IsUsed &&
                    t.ExpiresAt > DateTime.Now);

            if (resetToken == null)
            {
                TempData["ErrorMessage"] = "This reset link is invalid, expired, or has already been used. Please request a new one.";
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid reset request.";
                return RedirectToAction("ForgotPassword");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            if (!IsValidPassword(newPassword))
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters and contain an uppercase letter, a number, and a special character.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower() && u.IsActive);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid reset request.";
                return RedirectToAction("ForgotPassword");
            }

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.UserID == user.UserID &&
                    t.Token == token &&
                    !t.IsUsed &&
                    t.ExpiresAt > DateTime.Now);

            if (resetToken == null)
            {
                TempData["ErrorMessage"] = "This reset link is invalid, expired, or has already been used.";
                return RedirectToAction("ForgotPassword");
            }

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.ModifiedDate = DateTime.Now;
            user.IsPasswordChanged = true;

            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in with your new password.";
            return RedirectToAction("Login");
        }

        // ============================================
        // HELPERS
        // ============================================
        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private static bool IsValidPassword(string password)
        {
            return !string.IsNullOrEmpty(password) &&
                   password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        // ============================================
        // LOGOUT
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // ============================================
        // ACCESS DENIED
        // ============================================
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ============================================
        // ADMIN REGISTRATION
        // ============================================
        public IActionResult RegisterAdmin()
        {
            return View(new AdminRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin(AdminRegistrationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            const string ADMIN_SETUP_CODE = "NMBADMIN2025";

            if (model.AdminSetupCode != ADMIN_SETUP_CODE)
            {
                ModelState.AddModelError("AdminSetupCode", "Invalid admin setup code");
                return View(model);
            }

            try
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                var user = new User
                {
                    Name = model.FirstName.Trim(),
                    Surname = model.LastName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    PasswordHash = _passwordHasher.HashPassword(model.Password),
                    UserRole = "Admin",
                    IsActive = true,
                    IsPasswordChanged = true,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Admin registered successfully! Please login.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }
    }
}