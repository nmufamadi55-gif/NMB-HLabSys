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

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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
                var passwordHash = PasswordHelper.HashPassword(model.Password);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == passwordHash && u.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password");
                    return View(model);
                }

                // Set session
                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("UserRole", user.UserRole);
                HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}");

                // Redirect based on role
                if (user.UserRole == "Patient")
                {
                    return RedirectToAction("Dashboard", "Patient");
                }
                else if (user.UserRole == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (user.UserRole == "Doctor")
                {
                    return RedirectToAction("Index", "Home");
                }
                else if (user.UserRole == "Technician")
                {
                    return RedirectToAction("Index", "Home");
                }
                else if (user.UserRole == "Manager")
                {
                    return RedirectToAction("Index", "LaboratoryManager");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
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
                var passwordHash = PasswordHelper.HashPassword(model.Password);

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