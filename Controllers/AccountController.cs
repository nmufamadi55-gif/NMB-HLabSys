using NMB_HLabSys.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Helpers;
using NMB_HLabSys.Models;
using NMB_HLabSys.ViewModels.AdminViewModels;
using NMB_HLabSys.ViewModels.SharedViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // ============================================
        // LOGIN
        // ============================================
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email.ToLower() && u.IsActive);

                if (user == null || !_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                // ─── Silent rehash: upgrade legacy SHA256 → PBKDF2 on successful login ───
                if (_passwordHasher.NeedsRehash(user.PasswordHash))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(model.Password);
                    user.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Set session
                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("UserRole", user.UserRole);
                HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}");
                HttpContext.Session.SetString("UserEmail", user.Email);

                // Redirect based on role
                if (user.UserRole == "Patient")
                    return RedirectToAction("Dashboard", "Patient");
                else if (user.UserRole == "Admin")
                    return RedirectToAction("Dashboard", "Admin");
                else if (user.UserRole == "Doctor")
                    return RedirectToAction("Index", "Home");
                else if (user.UserRole == "Technician")
                    return RedirectToAction("Index", "Home");
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
        // FORGOT PASSWORD
        // ============================================

        // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Email is required";
                return View();
            }

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower() && u.IsActive);

                // Always show success (don't reveal if email exists)
                if (user != null)
                {
                    // Generate a temporary password
                    var newPassword = GenerateTemporaryPassword();
                    user.PasswordHash = _passwordHasher.HashPassword(newPassword);
                    user.IsPasswordChanged = false;
                    user.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // In production: send email with new password
                    // For now, show on screen (remove before publishing!)
                    ViewBag.TempPassword = newPassword;
                    ViewBag.FoundUser = true;
                }

                ViewBag.Success = "If this email is registered, a password reset link has been sent.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View();
            }
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
            var random = new Random();
            var password = new char[10];

            // Ensure at least one of each type
            password[0] = "ABCDEFGHJKLMNPQRSTUVWXYZ"[random.Next(24)];
            password[1] = "abcdefghijkmnpqrstuvwxyz"[random.Next(24)];
            password[2] = "23456789"[random.Next(8)];
            password[3] = "!@#$"[random.Next(4)];

            for (int i = 4; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            return new string(password);
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

        // GET: Account/RegisterAdmin
        public IActionResult RegisterAdmin()
        {
            return View(new AdminRegistrationViewModel());
        }

        // POST: Account/RegisterAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin(AdminRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify admin setup code
            const string ADMIN_SETUP_CODE = "NMBADMIN2025";

            if (model.AdminSetupCode != ADMIN_SETUP_CODE)
            {
                ModelState.AddModelError("AdminSetupCode", "Invalid admin setup code");
                return View(model);
            }

            try
            {
                // Check if email exists
                var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                // Hash password
                var passwordHash = _passwordHasher.HashPassword(model.Password);

                // Create admin user
                var user = new User
                {
                    Name = model.FirstName.Trim(),
                    Surname = model.LastName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    PasswordHash = passwordHash,
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