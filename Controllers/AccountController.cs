using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Helpers;
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

        // GET: Account/Login
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: Account/Login
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
                // Hash the password to compare with stored hash
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

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}