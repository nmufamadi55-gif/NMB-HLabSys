using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Services;
using NMB_HLabSys.ViewModels;
using System.Security.Claims;

namespace NMB_HLabSys.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IUserService userService,
            ILogger<HomeController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _userService = userService;
            _logger = logger;
        }

        // =========================
        // LOGIN - GET
        // =========================
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            // Always show login page - don't redirect if authenticated
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // =========================
        // LOGIN - POST
        // =========================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Find user by email
                var user = await _context.Users
                    .Include(u => u.LabTechnician)
                    .Include(u => u.Doctor)
                    .Include(u => u.Patient)
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                // Check if user exists and is active
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    _logger.LogWarning($"Login failed: User {model.Email} not found");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the administrator.");
                    _logger.LogWarning($"Login failed: User {model.Email} is inactive");
                    return View(model);
                }

                // Check lockout
                if (user.IsLockedOut && user.LockoutEndDate > DateTime.Now)
                {
                    var remainingMinutes = (int)(user.LockoutEndDate.Value - DateTime.Now).TotalMinutes;
                    ModelState.AddModelError(string.Empty, $"Account is locked. Please try again in {remainingMinutes} minutes.");
                    return View(model);
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    // Increment failed login attempts
                    user.FailedLoginAttempts++;

                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.IsLockedOut = true;
                        user.LockoutEndDate = DateTime.Now.AddMinutes(15);
                        await _context.SaveChangesAsync();
                        ModelState.AddModelError(string.Empty, "Account locked due to multiple failed attempts. Please try again in 15 minutes.");
                        _logger.LogWarning($"Account {model.Email} locked out after {user.FailedLoginAttempts} failed attempts");
                        return View(model);
                    }

                    await _context.SaveChangesAsync();
                    ModelState.AddModelError(string.Empty, $"Invalid email or password. {5 - user.FailedLoginAttempts} attempts remaining.");
                    _logger.LogWarning($"Login failed: Invalid password for {model.Email} (Attempt {user.FailedLoginAttempts})");
                    return View(model);
                }

                // Reset failed attempts on successful login
                user.FailedLoginAttempts = 0;
                user.IsLockedOut = false;
                user.LockoutEndDate = null;
                user.LastLoginDate = DateTime.Now;
                await _context.SaveChangesAsync();

                // Create authentication claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
                    new Claim(ClaimTypes.GivenName, user.Name),
                    new Claim(ClaimTypes.Surname, user.Surname),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.UserRole),
                    new Claim("UserId", user.UserID.ToString()),
                    new Claim("UserRole", user.UserRole)
                };

                // Add specific role IDs if applicable
                if (user.UserRole == "Technician" && user.LabTechnician != null)
                {
                    claims.Add(new Claim("TechnicianID", user.LabTechnician.TechnicianID.ToString()));
                }
                else if (user.UserRole == "Doctor" && user.Doctor != null)
                {
                    claims.Add(new Claim("DoctorID", user.Doctor.DoctorID.ToString()));
                }
                else if (user.UserRole == "Patient" && user.Patient != null)
                {
                    claims.Add(new Claim("PatientID", user.Patient.PatientID.ToString()));
                }

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

                _logger.LogInformation($"User {model.Email} logged in successfully");

                // Check if password change is required
                if (!user.IsPasswordChanged)
                {
                    TempData["RequiresPasswordChange"] = true;
                    return RedirectToAction("ChangePassword");
                }

                // Redirect to return URL or dashboard
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToDashboard();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for {model.Email}");
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        // =========================
        // LOGOUT
        // =========================
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"User {email} logged out");
            return RedirectToAction("Index");
        }

        // =========================
        // FORGOT PASSWORD - GET
        // =========================
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // =========================
        // FORGOT PASSWORD - POST
        // =========================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && user.IsActive)
                {
                    // Generate reset token
                    var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .Replace("=", "");

                    user.ResetToken = token;
                    user.ResetTokenExpiry = DateTime.Now.AddHours(24);
                    await _context.SaveChangesAsync();

                    // Send reset email
                    var resetLink = Url.Action("ResetPassword", "Home", new { token, email = user.Email }, Request.Scheme);

                    var subject = "Password Reset - NMB-HLabSys";
                    var body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Password Reset Request</h2>
                            <p>Dear {user.Name},</p>
                            <p>We received a request to reset your password for the NMB Haematology Laboratory Test Request Management System.</p>
                            <p>Click the link below to reset your password (valid for 24 hours):</p>
                            <p><a href='{resetLink}' style='background: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                            <p>If you did not request this, please ignore this email.</p>
                            <hr />
                            <p style='color: #6c757d; font-size: 12px;'>Nelson Mandela Bay Haematology Diagnostic Laboratories</p>
                            <p style='color: #6c757d; font-size: 12px;'>UN SDG 3: Good Health and Well-Being</p>
                        </body>
                        </html>
                    ";

                    await _emailService.SendEmailAsync(user.Email, subject, body);

                    _logger.LogInformation($"Password reset email sent to {model.Email}");
                    TempData["Success"] = "Password reset instructions have been sent to your email.";
                }
                else
                {
                    // Don't reveal if user exists for security
                    TempData["Success"] = "If your email is registered, you will receive password reset instructions.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing forgot password for {model.Email}");
                TempData["Error"] = "An error occurred processing your request. Please try again.";
                return View(model);
            }
        }

        // =========================
        // RESET PASSWORD - GET
        // =========================
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid password reset request.";
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.ResetToken != token || user.ResetTokenExpiry < DateTime.Now)
            {
                TempData["Error"] = "Invalid or expired password reset token. Please request a new one.";
                return RedirectToAction("ForgotPassword");
            }

            return View(new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            });
        }

        // =========================
        // RESET PASSWORD - POST
        // =========================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null || user.ResetToken != model.Token || user.ResetTokenExpiry < DateTime.Now)
                {
                    TempData["Error"] = "Invalid or expired password reset token.";
                    return RedirectToAction("ForgotPassword");
                }

                // Update password
                user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
                user.IsPasswordChanged = true;
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                user.PasswordChangedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password reset successful for {model.Email}");
                TempData["Success"] = "Your password has been reset successfully. Please login with your new password.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting password for {model.Email}");
                TempData["Error"] = "An error occurred resetting your password. Please try again.";
                return View(model);
            }
        }

        // =========================
        // CHANGE PASSWORD - GET
        // =========================
        [Authorize]
        public IActionResult ChangePassword()
        {
            var isFirstLogin = TempData["RequiresPasswordChange"] as bool? ?? false;
            var model = new ChangePasswordViewModel
            {
                IsFirstLogin = isFirstLogin
            };
            return View(model);
        }

        // =========================
        // CHANGE PASSWORD - POST
        // =========================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Verify current password (unless first login)
                if (!model.IsFirstLogin)
                {
                    if (!_passwordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                    {
                        ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
                        return View(model);
                    }
                }

                // Update password
                user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
                user.IsPasswordChanged = true;
                user.PasswordChangedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password changed for user {user.Email}");
                TempData["Success"] = "Password changed successfully!";

                // Re-authenticate
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                TempData["Error"] = "An error occurred changing your password. Please try again.";
                return View(model);
            }
        }

        // =========================
        // ACCESS DENIED
        // =========================
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================
        // HELPER METHODS
        // =========================
        private IActionResult RedirectToDashboard()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Manager";

            return role switch
            {
                "Manager" => RedirectToAction("Dashboard", "Manager"),
                "Technician" => RedirectToAction("Dashboard", "Technician"),
                "Doctor" => RedirectToAction("Dashboard", "Doctor"),
                "Patient" => RedirectToAction("Dashboard", "Patient"),
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                _ => RedirectToAction("Dashboard", "Manager")
            };
        }

        // =========================
        // ERROR
        // =========================
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}