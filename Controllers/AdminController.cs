using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.ViewModels.AdminViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NMB_HLabSys.Services;

namespace NMB_HLabSys.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AdminController(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // ============================================
        // DASHBOARD
        // ============================================
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var totalConditions = await _context.MedicalConditions.CountAsync(c => c.IsActive);
                var totalAllergies = await _context.Allergies.CountAsync(a => a.IsActive);
                var totalMedications = await _context.Medications.CountAsync(m => m.IsActive);
                var totalCategories = await _context.LookupCategories.CountAsync(c => c.IsActive);
                var totalPatients = await _context.Patients.CountAsync(p => p.IsActive);
                var totalDoctors = await _context.Doctors.CountAsync();
                var totalTechnicians = 0;

                var recentConditions = await _context.MedicalConditions
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.CreatedDate)
                    .Take(5)
                    .Select(c => new RecentActivityViewModel
                    {
                        Type = "Condition",
                        Name = c.ConditionName,
                        Action = "Added",
                        Date = c.CreatedDate,
                        UserName = "Admin"
                    }).ToListAsync();

                var recentAllergies = await _context.Allergies
                    .Where(a => a.IsActive)
                    .OrderByDescending(a => a.CreatedDate)
                    .Take(5)
                    .Select(a => new RecentActivityViewModel
                    {
                        Type = "Allergy",
                        Name = a.AllergyName,
                        Action = "Added",
                        Date = a.CreatedDate,
                        UserName = "Admin"
                    }).ToListAsync();

                var recentMedications = await _context.Medications
                    .Where(m => m.IsActive)
                    .OrderByDescending(m => m.CreatedDate)
                    .Take(5)
                    .Select(m => new RecentActivityViewModel
                    {
                        Type = "Medication",
                        Name = m.MedicationName,
                        Action = "Added",
                        Date = m.CreatedDate,
                        UserName = "Admin"
                    }).ToListAsync();

                var allActivities = recentConditions
                    .Concat(recentAllergies)
                    .Concat(recentMedications)
                    .OrderByDescending(a => a.Date)
                    .Take(10)
                    .ToList();

                var viewModel = new AdminDashboardViewModel
                {
                    TotalConditions = totalConditions,
                    TotalAllergies = totalAllergies,
                    TotalMedications = totalMedications,
                    TotalCategories = totalCategories,
                    TotalPatients = totalPatients,
                    TotalDoctors = totalDoctors,
                    TotalTechnicians = totalTechnicians,
                    RecentActivities = allActivities
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View(new AdminDashboardViewModel());
            }
        }

        // ============================================
        // ADMIN PROFILE
        // ============================================
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
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

            if (user.UserRole != "Admin")
                return RedirectToAction("Index", "Profile");

            var viewModel = new AdminProfileViewModel
            {
                UserID = user.UserID,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                ContactNumber = user.ContactNumber,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLogin = user.ModifiedDate
            };

            return View(viewModel);
        }

        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
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

            if (user.UserRole != "Admin")
                return RedirectToAction("Index", "Profile");

            var viewModel = new AdminProfileViewModel
            {
                UserID = user.UserID,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                ContactNumber = user.ContactNumber,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLogin = user.ModifiedDate
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(AdminProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (user.UserRole != "Admin")
                return RedirectToAction("Index", "Profile");

            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Surname))
            {
                ModelState.AddModelError("", "Name and Surname are required.");
                return View(model);
            }

            var newEmail = (model.Email ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(model);
            }

            if (newEmail != user.Email.ToLower())
            {
                var emailTaken = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == newEmail && u.UserID != user.UserID);

                if (emailTaken)
                {
                    ModelState.AddModelError("Email", "This email is already registered to another account.");
                    return View(model);
                }
            }

            user.Name = model.Name.Trim();
            user.Surname = model.Surname.Trim();
            user.Email = newEmail;
            user.ContactNumber = model.ContactNumber?.Trim();
            user.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserEmail", newEmail);
            HttpContext.Session.SetString("UserName", $"{user.Name} {user.Surname}");

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAdminPassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (user.UserRole != "Admin")
                return RedirectToAction("Index", "Profile");

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                TempData["ErrorMessage"] = "Current password is required.";
                return RedirectToAction("Profile");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "New password must be at least 8 characters.";
                return RedirectToAction("Profile");
            }

            if (!IsPasswordComplex(newPassword))
            {
                TempData["ErrorMessage"] = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.";
                return RedirectToAction("Profile");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToAction("Profile");
            }

            if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("Profile");
            }

            if (_passwordHasher.VerifyPassword(newPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "New password must be different from the current one.";
                return RedirectToAction("Profile");
            }

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.IsPasswordChanged = true;
            user.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Profile");
        }

        private static bool IsPasswordComplex(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            return password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        // ============================================
        // CATEGORIES CRUD
        // ============================================
        public async Task<IActionResult> Categories(bool showDeleted = false)
        {
            try
            {
                var query = _context.LookupCategories.AsQueryable();

                if (!showDeleted)
                    query = query.Where(c => c.IsActive);

                var categories = await query
                    .OrderByDescending(c => c.IsActive)
                    .ThenBy(c => c.CategoryType)
                    .ThenBy(c => c.CategoryName)
                    .ToListAsync();

                ViewBag.ShowDeleted = showDeleted;
                return View(categories);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                ViewBag.ShowDeleted = showDeleted;
                return View(new List<LookupCategory>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string categoryName, string categoryType, string description)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["ErrorMessage"] = "Category name is required";
                return RedirectToAction("Categories");
            }

            if (string.IsNullOrWhiteSpace(categoryType))
            {
                TempData["ErrorMessage"] = "Category type is required";
                return RedirectToAction("Categories");
            }

            var validCategoryTypes = new[] { "Condition", "Allergy", "Medication" };
            if (!validCategoryTypes.Contains(categoryType))
            {
                TempData["ErrorMessage"] = "❌ Invalid category type. Must be Condition, Allergy, or Medication.";
                return RedirectToAction("Categories");
            }

            try
            {
                var trimmedName = categoryName.Trim();

                var existing = await _context.LookupCategories
                    .FirstOrDefaultAsync(c => c.CategoryName == trimmedName && c.CategoryType == categoryType);

                if (existing != null)
                {
                    if (existing.IsActive)
                    {
                        TempData["ErrorMessage"] = $"❌ '{trimmedName}' already exists!";
                        return RedirectToAction("Categories");
                    }

                    existing.IsActive = true;
                    existing.Description = description?.Trim();
                    existing.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"✅ Category '{trimmedName}' reactivated successfully!";
                    return RedirectToAction("Categories");
                }

                var category = new LookupCategory
                {
                    CategoryName = trimmedName,
                    CategoryType = categoryType,
                    Description = description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.LookupCategories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Category '{trimmedName}' added successfully!";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message} | Inner: {innerMessage}";
            }

            return RedirectToAction("Categories");
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.LookupCategories.FindAsync(id);
            if (category == null) return NotFound();

            var viewModel = new ManageCategoryViewModel
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName,
                CategoryType = category.CategoryType,
                Description = category.Description,
                IsActive = category.IsActive
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(ManageCategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var validCategoryTypes = new[] { "Condition", "Allergy", "Medication" };
            if (!validCategoryTypes.Contains(model.CategoryType))
            {
                ModelState.AddModelError("CategoryType", "Invalid category type. Must be Condition, Allergy, or Medication.");
                return View(model);
            }

            try
            {
                var category = await _context.LookupCategories.FindAsync(model.CategoryID);
                if (category == null) return NotFound();

                var trimmedName = model.CategoryName.Trim();

                var duplicate = await _context.LookupCategories
                    .FirstOrDefaultAsync(c =>
                        c.CategoryName == trimmedName &&
                        c.CategoryType == model.CategoryType &&
                        c.CategoryID != model.CategoryID);

                if (duplicate != null)
                {
                    TempData["ErrorMessage"] = $"❌ A category named '{trimmedName}' already exists for type '{model.CategoryType}'.";
                    return View(model);
                }

                category.CategoryName = trimmedName;
                category.CategoryType = model.CategoryType;
                category.Description = model.Description?.Trim();
                category.IsActive = model.IsActive;
                category.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Category '{trimmedName}' updated successfully!";
                return RedirectToAction("Categories");
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message} | Inner: {innerMessage}";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.LookupCategories.FindAsync(id);
                if (category != null)
                {
                    category.IsActive = false;
                    category.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{category.CategoryName}' deleted (soft)" });
                }
                return Json(new { success = false, message = "Category not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateCategory(int id)
        {
            try
            {
                var category = await _context.LookupCategories.FindAsync(id);
                if (category != null)
                {
                    category.IsActive = true;
                    category.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{category.CategoryName}' reactivated" });
                }
                return Json(new { success = false, message = "Category not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // CONDITIONS CRUD
        // ============================================
        public async Task<IActionResult> Conditions(bool showDeleted = false)
        {
            var conditionsQuery = _context.MedicalConditions
                .Include(c => c.Category)
                .AsQueryable();

            if (!showDeleted)
                conditionsQuery = conditionsQuery.Where(c => c.IsActive);

            var conditions = await conditionsQuery
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.ConditionName)
                .ToListAsync();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Condition" && c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.ShowDeleted = showDeleted;

            var viewModel = new AdminLookupViewModel
            {
                Conditions = conditions,
                Categories = categories,
                SelectedCategoryType = "Condition"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCondition(string conditionName, int categoryId, string description)
        {
            if (string.IsNullOrWhiteSpace(conditionName))
            {
                TempData["ErrorMessage"] = "Condition name is required";
                return RedirectToAction("Conditions");
            }

            // Validate category exists, is active, and is the right type
            var category = await _context.LookupCategories
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId
                                       && c.CategoryType == "Condition"
                                       && c.IsActive);

            if (category == null)
            {
                TempData["ErrorMessage"] = "❌ Please select a valid active Condition category.";
                return RedirectToAction("Conditions");
            }

            try
            {
                var trimmedName = conditionName.Trim();

                var existing = await _context.MedicalConditions
                    .FirstOrDefaultAsync(c => c.ConditionName == trimmedName);

                if (existing != null)
                {
                    if (existing.IsActive)
                    {
                        TempData["ErrorMessage"] = $"❌ '{trimmedName}' already exists!";
                        return RedirectToAction("Conditions");
                    }

                    existing.IsActive = true;
                    existing.CategoryID = categoryId;
                    existing.Description = description?.Trim();
                    existing.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"✅ Condition '{trimmedName}' reactivated successfully!";
                    return RedirectToAction("Conditions");
                }

                var condition = new MedicalCondition
                {
                    ConditionName = trimmedName,
                    CategoryID = categoryId,
                    Description = description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.MedicalConditions.Add(condition);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Condition '{trimmedName}' added successfully!";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message} | Inner: {innerMessage}";
            }

            return RedirectToAction("Conditions");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCondition(int id)
        {
            try
            {
                var condition = await _context.MedicalConditions.FindAsync(id);
                if (condition != null)
                {
                    condition.IsActive = false;
                    condition.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{condition.ConditionName}' deleted (soft)" });
                }
                return Json(new { success = false, message = "Condition not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateCondition(int id)
        {
            try
            {
                var condition = await _context.MedicalConditions.FindAsync(id);
                if (condition != null)
                {
                    condition.IsActive = true;
                    condition.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{condition.ConditionName}' reactivated" });
                }
                return Json(new { success = false, message = "Condition not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> EditCondition(int id)
        {
            var condition = await _context.MedicalConditions.FindAsync(id);
            if (condition == null) return NotFound();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Condition" && c.IsActive)
                .ToListAsync();

            var viewModel = new ManageConditionViewModel
            {
                ConditionID = condition.ConditionID,
                ConditionName = condition.ConditionName,
                CategoryID = condition.CategoryID,
                Description = condition.Description,
                IsActive = condition.IsActive,
                Categories = categories
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCondition(ManageConditionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.LookupCategories
                    .Where(c => c.CategoryType == "Condition" && c.IsActive)
                    .ToListAsync();
                return View(model);
            }

            try
            {
                var condition = await _context.MedicalConditions.FindAsync(model.ConditionID);
                if (condition == null) return NotFound();

                condition.ConditionName = model.ConditionName.Trim();
                condition.CategoryID = model.CategoryID;
                condition.Description = model.Description?.Trim();
                condition.IsActive = model.IsActive;
                condition.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Condition '{model.ConditionName}' updated successfully!";
                return RedirectToAction("Conditions");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
                return View(model);
            }
        }

        // ============================================
        // ALLERGIES CRUD
        // ============================================
        public async Task<IActionResult> Allergies(bool showDeleted = false)
        {
            var allergiesQuery = _context.Allergies
                .Include(a => a.Category)
                .AsQueryable();

            if (!showDeleted)
                allergiesQuery = allergiesQuery.Where(a => a.IsActive);

            var allergies = await allergiesQuery
                .OrderByDescending(a => a.IsActive)
                .ThenBy(a => a.AllergyName)
                .ToListAsync();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Allergy" && c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.ShowDeleted = showDeleted;

            var viewModel = new AdminLookupViewModel
            {
                Allergies = allergies,
                Categories = categories,
                SelectedCategoryType = "Allergy"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAllergy(string allergyName, int categoryId, string description)
        {
            if (string.IsNullOrWhiteSpace(allergyName))
            {
                TempData["ErrorMessage"] = "Allergy name is required";
                return RedirectToAction("Allergies");
            }

            // Validate category exists, is active, and is the right type
            var category = await _context.LookupCategories
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId
                                       && c.CategoryType == "Allergy"
                                       && c.IsActive);

            if (category == null)
            {
                TempData["ErrorMessage"] = "❌ Please select a valid active Allergy category.";
                return RedirectToAction("Allergies");
            }

            try
            {
                var trimmedName = allergyName.Trim();

                var existing = await _context.Allergies
                    .FirstOrDefaultAsync(a => a.AllergyName == trimmedName);

                if (existing != null)
                {
                    if (existing.IsActive)
                    {
                        TempData["ErrorMessage"] = $"❌ '{trimmedName}' already exists!";
                        return RedirectToAction("Allergies");
                    }

                    existing.IsActive = true;
                    existing.CategoryID = categoryId;
                    existing.Description = description?.Trim();
                    existing.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"✅ Allergy '{trimmedName}' reactivated successfully!";
                    return RedirectToAction("Allergies");
                }

                var allergy = new Allergy
                {
                    AllergyName = trimmedName,
                    CategoryID = categoryId,
                    Description = description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Allergies.Add(allergy);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Allergy '{trimmedName}' added successfully!";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message} | Inner: {innerMessage}";
            }

            return RedirectToAction("Allergies");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllergy(int id)
        {
            try
            {
                var allergy = await _context.Allergies.FindAsync(id);
                if (allergy != null)
                {
                    allergy.IsActive = false;
                    allergy.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{allergy.AllergyName}' deleted (soft)" });
                }
                return Json(new { success = false, message = "Allergy not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateAllergy(int id)
        {
            try
            {
                var allergy = await _context.Allergies.FindAsync(id);
                if (allergy != null)
                {
                    allergy.IsActive = true;
                    allergy.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{allergy.AllergyName}' reactivated" });
                }
                return Json(new { success = false, message = "Allergy not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> EditAllergy(int id)
        {
            var allergy = await _context.Allergies.FindAsync(id);
            if (allergy == null) return NotFound();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Allergy" && c.IsActive)
                .ToListAsync();

            var viewModel = new ManageAllergyViewModel
            {
                AllergyID = allergy.AllergyID,
                AllergyName = allergy.AllergyName,
                CategoryID = allergy.CategoryID,
                Description = allergy.Description,
                IsActive = allergy.IsActive,
                Categories = categories
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAllergy(ManageAllergyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.LookupCategories
                    .Where(c => c.CategoryType == "Allergy" && c.IsActive)
                    .ToListAsync();
                return View(model);
            }

            try
            {
                var allergy = await _context.Allergies.FindAsync(model.AllergyID);
                if (allergy == null) return NotFound();

                allergy.AllergyName = model.AllergyName.Trim();
                allergy.CategoryID = model.CategoryID;
                allergy.Description = model.Description?.Trim();
                allergy.IsActive = model.IsActive;
                allergy.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Allergy '{model.AllergyName}' updated successfully!";
                return RedirectToAction("Allergies");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
                return View(model);
            }
        }

        // ============================================
        // MEDICATIONS CRUD
        // ============================================
        public async Task<IActionResult> Medications(bool showDeleted = false)
        {
            var medicationsQuery = _context.Medications
                .Include(m => m.Category)
                .AsQueryable();

            if (!showDeleted)
                medicationsQuery = medicationsQuery.Where(m => m.IsActive);

            var medications = await medicationsQuery
                .OrderByDescending(m => m.IsActive)
                .ThenBy(m => m.MedicationName)
                .ToListAsync();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Medication" && c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.ShowDeleted = showDeleted;

            var viewModel = new AdminLookupViewModel
            {
                Medications = medications,
                Categories = categories,
                SelectedCategoryType = "Medication"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedication(string medicationName, int categoryId, string description)
        {
            if (string.IsNullOrWhiteSpace(medicationName))
            {
                TempData["ErrorMessage"] = "Medication name is required";
                return RedirectToAction("Medications");
            }

            // Validate category exists, is active, and is the right type
            var category = await _context.LookupCategories
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId
                                       && c.CategoryType == "Medication"
                                       && c.IsActive);

            if (category == null)
            {
                TempData["ErrorMessage"] = "❌ Please select a valid active Medication category.";
                return RedirectToAction("Medications");
            }

            try
            {
                var trimmedName = medicationName.Trim();

                var existing = await _context.Medications
                    .FirstOrDefaultAsync(m => m.MedicationName == trimmedName);

                if (existing != null)
                {
                    if (existing.IsActive)
                    {
                        TempData["ErrorMessage"] = $"❌ '{trimmedName}' already exists!";
                        return RedirectToAction("Medications");
                    }

                    existing.IsActive = true;
                    existing.CategoryID = categoryId;
                    existing.Description = description?.Trim();
                    existing.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"✅ Medication '{trimmedName}' reactivated successfully!";
                    return RedirectToAction("Medications");
                }

                var medication = new Medication
                {
                    MedicationName = trimmedName,
                    CategoryID = categoryId,
                    Description = description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Medications.Add(medication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Medication '{trimmedName}' added successfully!";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message} | Inner: {innerMessage}";
            }

            return RedirectToAction("Medications");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            try
            {
                var medication = await _context.Medications.FindAsync(id);
                if (medication != null)
                {
                    medication.IsActive = false;
                    medication.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{medication.MedicationName}' deleted (soft)" });
                }
                return Json(new { success = false, message = "Medication not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateMedication(int id)
        {
            try
            {
                var medication = await _context.Medications.FindAsync(id);
                if (medication != null)
                {
                    medication.IsActive = true;
                    medication.ModifiedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{medication.MedicationName}' reactivated" });
                }
                return Json(new { success = false, message = "Medication not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> EditMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);
            if (medication == null) return NotFound();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Medication" && c.IsActive)
                .ToListAsync();

            var viewModel = new ManageMedicationViewModel
            {
                MedicationID = medication.MedicationID,
                MedicationName = medication.MedicationName,
                CategoryID = medication.CategoryID,
                Description = medication.Description,
                IsActive = medication.IsActive,
                Categories = categories
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedication(ManageMedicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.LookupCategories
                    .Where(c => c.CategoryType == "Medication" && c.IsActive)
                    .ToListAsync();
                return View(model);
            }

            try
            {
                var medication = await _context.Medications.FindAsync(model.MedicationID);
                if (medication == null) return NotFound();

                medication.MedicationName = model.MedicationName.Trim();
                medication.CategoryID = model.CategoryID;
                medication.Description = model.Description?.Trim();
                medication.IsActive = model.IsActive;
                medication.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Medication '{model.MedicationName}' updated successfully!";
                return RedirectToAction("Medications");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
                return View(model);
            }
        }
    }
}