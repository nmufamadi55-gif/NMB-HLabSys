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

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, [FromServices] IEmailService emailService)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email is required.";
                return View();
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower() && u.IsActive);

                // Always show success to prevent email harvesting (security best practice)
                if (user != null)
                {
                    // Generate a secure, unique reset token
                    var token = Guid.NewGuid().ToString("N");

                    // Store the token in TempData bound to the user's email
                    // In a real production app, you would save this token to the database with an expiry date.
                    TempData[$"ResetToken_{user.Email}"] = token;

                    // Build the reset link
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var resetLink = $"{baseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

                    // Send the email
                    try
                    {
                        await emailService.SendPasswordResetEmailAsync(
                            user.Email,
                            $"{user.Name} {user.Surname}".Trim(),
                            resetLink);
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't reveal to the user that the email failed
                        Console.WriteLine($"Email failed: {ex.Message}");
                    }
                }

                TempData["SuccessMessage"] = "If this email is registered, a password reset link has been sent to it. Check your inbox (and spam folder).";
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View();
            }
        }

        // GET: Account/ResetPassword?email=...&token=...
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Invalid reset link.";
                return RedirectToAction("Login");
            }

            // Verify token exists in TempData
            var storedToken = TempData[$"ResetToken_{email}"] as string;

            // Put the token back in TempData so the POST method can read it again
            if (storedToken != null) TempData[$"ResetToken_{email}"] = storedToken;

            if (storedToken != token)
            {
                TempData["ErrorMessage"] = "This reset link is invalid or has expired. Please request a new one.";
                return View();
            }

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Invalid reset request.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    newPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"))
            {
                TempData["ErrorMessage"] = "Password must contain uppercase, lowercase, number, and special character.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            // Verify token
            var storedToken = TempData[$"ResetToken_{email}"] as string;
            if (storedToken != token)
            {
                TempData["ErrorMessage"] = "This reset link is invalid or has expired.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction("Login");
            }

            // Update the password
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.IsPasswordChanged = true;
            user.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // Invalidate the token
            TempData.Remove($"ResetToken_{email}");

            TempData["SuccessMessage"] = "Password reset successfully. Please log in with your new password.";
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