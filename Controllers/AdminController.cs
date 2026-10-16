using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.ViewModels.AdminViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================================
        // DASHBOARD
        // ============================================
        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Real counts from DB
                var totalConditions = await _context.MedicalConditions.CountAsync(c => c.IsActive);
                var totalAllergies = await _context.Allergies.CountAsync(a => a.IsActive);
                var totalMedications = await _context.Medications.CountAsync(m => m.IsActive);
                var totalCategories = await _context.LookupCategories.CountAsync(c => c.IsActive);
                var totalPatients = await _context.Patients.CountAsync(p => p.IsActive);
                var totalDoctors = await _context.Doctors.CountAsync();
                var totalTechnicians = 0;

                // Real recent activities from DB
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
        // CATEGORIES CRUD
        // ============================================
        public async Task<IActionResult> Categories()
        {
            try
            {
                var categories = await _context.LookupCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryType)
                    .ThenBy(c => c.CategoryName)
                    .ToListAsync();

                return View(categories);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
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

            try
            {
                var trimmedName = categoryName.Trim();

                // Check if category already exists (active OR inactive)
                var existing = await _context.LookupCategories
                    .FirstOrDefaultAsync(c => c.CategoryName == trimmedName && c.CategoryType == categoryType);

                if (existing != null)
                {
                    if (existing.IsActive)
                    {
                        TempData["ErrorMessage"] = $"❌ '{trimmedName}' already exists!";
                        return RedirectToAction("Categories");
                    }

                    // Reactivate
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

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.LookupCategories.FindAsync(id);
                if (category != null)
                {
                    category.IsActive = false;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{category.CategoryName}' deleted" });
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
        public async Task<IActionResult> Conditions()
        {
            var conditions = await _context.MedicalConditions
                .Include(c => c.Category)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ConditionName)
                .ToListAsync();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Condition" && c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

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

                    // Reactivate
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
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{condition.ConditionName}' deleted" });
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
            if (condition == null)
            {
                return NotFound();
            }

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
                if (condition == null)
                {
                    return NotFound();
                }

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
        public async Task<IActionResult> Allergies()
        {
            var allergies = await _context.Allergies
                .Include(a => a.Category)
                .Where(a => a.IsActive)
                .OrderBy(a => a.AllergyName)
                .ToListAsync();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Allergy" && c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

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

                    // Reactivate
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
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{allergy.AllergyName}' deleted" });
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
            if (allergy == null)
            {
                return NotFound();
            }

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
                if (allergy == null)
                {
                    return NotFound();
                }

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
        public async Task<IActionResult> Medications()
        {
            var medications = await _context.Medications
                .Include(m => m.Category)
                .Where(m => m.IsActive)
                .OrderBy(m => m.MedicationName)
                .ToListAsync();

            var categories = await _context.LookupCategories
                .Where(c => c.CategoryType == "Medication" && c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

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

                    // Reactivate
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
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"✅ '{medication.MedicationName}' deleted" });
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
            if (medication == null)
            {
                return NotFound();
            }

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
                if (medication == null)
                {
                    return NotFound();
                }

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