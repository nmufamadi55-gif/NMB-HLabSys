using Microsoft.AspNetCore.Mvc;

namespace NMB_HLabSys.Controllers
{
    public class ProfileController : Controller
    {
        // For prototype - store user info in ViewBag without session
        // In production, this would come from database via session or claims

        public IActionResult Index()
        {
            // For prototype, determine role from URL or use default
            // You can also pass role via query parameter or set a cookie

            var userRole = "Doctor"; // Default role for prototype
            var userEmail = "doctor@nmblab.com";

            // Optional: Check if coming from a specific controller
            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("/Technician")) userRole = "Technician";
            else if (referer.Contains("/Manager")) userRole = "Manager";
            else if (referer.Contains("/Patient")) userRole = "Patient";
            else if (referer.Contains("/Admin")) userRole = "Admin";
            else if (referer.Contains("/Doctor")) userRole = "Doctor";

            // You can also accept role as query parameter
            if (Request.Query.ContainsKey("role"))
            {
                userRole = Request.Query["role"].ToString();
            }

            ViewBag.UserRole = userRole;
            ViewBag.UserEmail = userEmail;
            ViewBag.UserName = GetUserNameFromRole(userRole);
            ViewBag.UserInitials = GetInitials(ViewBag.UserName);
            ViewBag.UserGreeting = GetUserGreeting(userRole, ViewBag.UserName);

            return View();
        }

        [HttpPost]
        public IActionResult UpdateProfile(string name, string surname, string email, string phone)
        {
            // In production, update database
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
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

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Index");
        }

        private bool IsValidPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        private string GetUserNameFromRole(string role)
        {
            switch (role)
            {
                case "Doctor": return "Dr. Sarah Johnson";
                case "Technician": return "Michael Peters";
                case "Manager": return "Amanda Adams";
                case "Patient": return "Thabo Mbeki";
                case "Admin": return "Admin User";
                default: return "User";
            }
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "U";
            var parts = name.Split(' ');
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