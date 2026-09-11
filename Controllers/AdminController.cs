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
        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalConditions = await _context.MedicalConditions.CountAsync(c => c.IsActive),
                TotalAllergies = await _context.Allergies.CountAsync(a => a.IsActive),
                TotalMedications = await _context.Medications.CountAsync(m => m.IsActive),
                TotalCategories = await _context.LookupCategories.CountAsync(c => c.IsActive),
                TotalPatients = await _context.Patients.CountAsync(p => p.IsActive),
                TotalDoctors = await _context.Doctors.CountAsync(d => d.IsVerified),
                TotalTechnicians = 0
            };

            return View(viewModel);
        }

        // ============================================
        // CATEGORIES CRUD
        // ============================================

        // GET: Admin/Categories
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

        // POST: Admin/AddCategory
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
                var category = new LookupCategory
                {
                    CategoryName = categoryName.Trim(),
                    CategoryType = categoryType,
                    Description = description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.LookupCategories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Category '{categoryName}' added successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
            }

            return RedirectToAction("Categories");
        }

        // POST: Admin/DeleteCategory
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
                var condition = new MedicalCondition
                {
                    ConditionName = conditionName.Trim(),
                    CategoryID = categoryId,
                    Description = description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.MedicalConditions.Add(condition);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Condition '{conditionName}' added successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
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

        // GET: Admin/EditCondition/{id}
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

                TempData["SuccessMessage"] = $"✅ Condition updated successfully!";
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
                var allergy = new Allergy
                {
                    AllergyName = allergyName.Trim(),
                    CategoryID = categoryId,
                    Description = description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Allergies.Add(allergy);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Allergy '{allergyName}' added successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
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

                TempData["SuccessMessage"] = $"✅ Allergy updated successfully!";
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
                var medication = new Medication
                {
                    MedicationName = medicationName.Trim(),
                    CategoryID = categoryId,
                    Description = description?.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Medications.Add(medication);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✅ Medication '{medicationName}' added successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
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

                TempData["SuccessMessage"] = $"✅ Medication updated successfully!";
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