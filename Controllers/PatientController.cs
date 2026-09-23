using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Helpers;
using NMB_HLabSys.Models;
using NMB_HLabSys.ViewModels.PatientViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET LOGGED-IN PATIENT
        private async Task<Patient> GetLoggedInPatient()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue) return null;

            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == userId.Value && p.IsActive);
        }

        // DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var viewModel = new PatientDashboardViewModel
            {
                PatientID = patient.PatientID,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                SAIDNumber = patient.SAIDNumber,
                Email = patient.User?.Email ?? patient.Email,
                Cellphone = patient.Cellphone,
                DateOfBirth = patient.DateOfBirth,
                TotalTestRequests = 0,
                AbnormalResultsCount = 0,
                ReportsAvailable = 0,
                HealthScore = 0,
                HealthStatusMessage = "No health data yet.",
                CholesterolTrend = "No data",
                RecentTestResults = new List<RecentTestResultViewModel>(),
                UpcomingTests = new List<UpcomingTestViewModel>()
            };
            return View(viewModel);
        }

        // PROFILE
        public async Task<IActionResult> Profile()
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var viewModel = new PatientProfileViewModel
            {
                PatientID = patient.PatientID,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                SAIDNumber = patient.SAIDNumber,
                Email = patient.User?.Email ?? patient.Email,
                Cellphone = patient.Cellphone,
                HomeAddress = patient.HomeAddress,
                DateOfBirth = patient.DateOfBirth,
                Conditions = await _context.PatientConditions
                    .Include(pc => pc.Condition).ThenInclude(c => c.Category)
                    .Where(pc => pc.PatientID == patient.PatientID && pc.IsActive)
                    .Select(pc => new PatientConditionViewModel
                    {
                        PatientConditionID = pc.PatientConditionID,
                        ConditionName = pc.Condition.ConditionName,
                        CategoryName = pc.Condition.Category != null ? pc.Condition.Category.CategoryName : "",
                        DiagnosedDate = pc.DiagnosedDate,
                        Notes = pc.Notes,
                        IsActive = pc.IsActive
                    }).ToListAsync(),
                Allergies = await _context.PatientAllergies
                    .Include(pa => pa.Allergy).ThenInclude(a => a.Category)
                    .Where(pa => pa.PatientID == patient.PatientID)
                    .Select(pa => new PatientAllergyViewModel
                    {
                        PatientAllergyID = pa.PatientAllergyID,
                        AllergyName = pa.Allergy.AllergyName,
                        CategoryName = pa.Allergy.Category != null ? pa.Allergy.Category.CategoryName : "",
                        Severity = pa.Severity,
                        Notes = pa.Notes
                    }).ToListAsync(),
                Medications = await _context.PatientMedications
                    .Include(pm => pm.Medication).ThenInclude(m => m.Category)
                    .Where(pm => pm.PatientID == patient.PatientID)
                    .Select(pm => new PatientMedicationViewModel
                    {
                        PatientMedicationID = pm.PatientMedicationID,
                        MedicationName = pm.Medication.MedicationName,
                        CategoryName = pm.Medication.Category != null ? pm.Medication.Category.CategoryName : "",
                        Dosage = pm.Dosage,
                        Frequency = pm.Frequency,
                        StartDate = pm.StartDate,
                        EndDate = pm.EndDate,
                        IsCurrent = pm.IsCurrent,
                        Notes = pm.Notes
                    }).ToListAsync()
            };
            return View(viewModel);
        }

        // EDIT PROFILE
        public async Task<IActionResult> EditProfile()
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            return View(new EditPatientProfileViewModel
            {
                PatientID = patient.PatientID,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Cellphone = patient.Cellphone,
                HomeAddress = patient.HomeAddress,
                Email = patient.User?.Email ?? patient.Email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditPatientProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            patient.FirstName = model.FirstName.Trim();
            patient.LastName = model.LastName.Trim();
            patient.Cellphone = model.Cellphone.Trim();
            patient.HomeAddress = model.HomeAddress?.Trim();
            patient.ModifiedDate = DateTime.Now;

            if (patient.User != null)
            {
                patient.User.Name = model.FirstName.Trim();
                patient.User.Surname = model.LastName.Trim();
                patient.User.ContactNumber = model.Cellphone.Trim();
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated!";
            return RedirectToAction("Profile");
        }

        // MEDICAL HISTORY
        public async Task<IActionResult> MedicalHistory()
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var viewModel = new PatientMedicalHistoryViewModel
            {
                PatientID = patient.PatientID,
                FullName = $"{patient.FirstName} {patient.LastName}",
                Conditions = await _context.PatientConditions
                    .Include(pc => pc.Condition).ThenInclude(c => c.Category)
                    .Where(pc => pc.PatientID == patient.PatientID && pc.IsActive)
                    .Select(pc => new PatientConditionViewModel
                    {
                        PatientConditionID = pc.PatientConditionID,
                        ConditionName = pc.Condition.ConditionName,
                        CategoryName = pc.Condition.Category != null ? pc.Condition.Category.CategoryName : "",
                        DiagnosedDate = pc.DiagnosedDate,
                        Notes = pc.Notes,
                        IsActive = pc.IsActive
                    }).ToListAsync(),
                Allergies = await _context.PatientAllergies
                    .Include(pa => pa.Allergy).ThenInclude(a => a.Category)
                    .Where(pa => pa.PatientID == patient.PatientID)
                    .Select(pa => new PatientAllergyViewModel
                    {
                        PatientAllergyID = pa.PatientAllergyID,
                        AllergyName = pa.Allergy.AllergyName,
                        CategoryName = pa.Allergy.Category != null ? pa.Allergy.Category.CategoryName : "",
                        Severity = pa.Severity,
                        Notes = pa.Notes
                    }).ToListAsync(),
                Medications = await _context.PatientMedications
                    .Include(pm => pm.Medication).ThenInclude(m => m.Category)
                    .Where(pm => pm.PatientID == patient.PatientID)
                    .Select(pm => new PatientMedicationViewModel
                    {
                        PatientMedicationID = pm.PatientMedicationID,
                        MedicationName = pm.Medication.MedicationName,
                        CategoryName = pm.Medication.Category != null ? pm.Medication.Category.CategoryName : "",
                        Dosage = pm.Dosage,
                        Frequency = pm.Frequency,
                        StartDate = pm.StartDate,
                        EndDate = pm.EndDate,
                        IsCurrent = pm.IsCurrent,
                        Notes = pm.Notes
                    }).ToListAsync(),
                AvailableConditions = await _context.MedicalConditions.Where(c => c.IsActive).ToListAsync(),
                AvailableAllergies = await _context.Allergies.Where(a => a.IsActive).ToListAsync(),
                AvailableMedications = await _context.Medications.Where(m => m.IsActive).ToListAsync()
            };
            return View(viewModel);
        }

        // ADD CONDITION
        [HttpPost]
        public async Task<IActionResult> AddCondition(int conditionId, DateTime? diagnosedDate, string notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var exists = await _context.PatientConditions
                .AnyAsync(pc => pc.PatientID == patient.PatientID && pc.ConditionID == conditionId && pc.IsActive);
            if (exists) return Json(new { success = false, message = "Already have this condition" });

            _context.PatientConditions.Add(new PatientCondition
            {
                PatientID = patient.PatientID,
                ConditionID = conditionId,
                DiagnosedDate = diagnosedDate,
                Notes = notes,
                IsActive = true,
                CreatedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Condition added!" });
        }

        // REMOVE CONDITION
        [HttpPost]
        public async Task<IActionResult> RemoveCondition(int patientConditionId)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var pc = await _context.PatientConditions
                .FirstOrDefaultAsync(x => x.PatientConditionID == patientConditionId && x.PatientID == patient.PatientID);
            if (pc == null) return Json(new { success = false, message = "Not found" });

            _context.PatientConditions.Remove(pc);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Condition removed!" });
        }

        // ADD ALLERGY
        [HttpPost]
        public async Task<IActionResult> AddAllergy(int allergyId, string severity, string notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var exists = await _context.PatientAllergies
                .AnyAsync(pa => pa.PatientID == patient.PatientID && pa.AllergyID == allergyId);
            if (exists) return Json(new { success = false, message = "Already have this allergy" });

            _context.PatientAllergies.Add(new PatientAllergy
            {
                PatientID = patient.PatientID,
                AllergyID = allergyId,
                Severity = severity,
                Notes = notes,
                CreatedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Allergy added!" });
        }

        // REMOVE ALLERGY
        [HttpPost]
        public async Task<IActionResult> RemoveAllergy(int patientAllergyId)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var pa = await _context.PatientAllergies
                .FirstOrDefaultAsync(x => x.PatientAllergyID == patientAllergyId && x.PatientID == patient.PatientID);
            if (pa == null) return Json(new { success = false, message = "Not found" });

            _context.PatientAllergies.Remove(pa);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Allergy removed!" });
        }

        // ADD MEDICATION
        [HttpPost]
        public async Task<IActionResult> AddMedication(int medicationId, string dosage, string frequency, DateTime? startDate, DateTime? endDate, string notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var exists = await _context.PatientMedications
                .AnyAsync(pm => pm.PatientID == patient.PatientID && pm.MedicationID == medicationId);
            if (exists) return Json(new { success = false, message = "Already have this medication" });

            _context.PatientMedications.Add(new PatientMedication
            {
                PatientID = patient.PatientID,
                MedicationID = medicationId,
                Dosage = dosage,
                Frequency = frequency,
                StartDate = startDate,
                EndDate = endDate,
                IsCurrent = endDate == null,
                Notes = notes,
                CreatedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Medication added!" });
        }

        // REMOVE MEDICATION
        [HttpPost]
        public async Task<IActionResult> RemoveMedication(int patientMedicationId)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var pm = await _context.PatientMedications
                .FirstOrDefaultAsync(x => x.PatientMedicationID == patientMedicationId && x.PatientID == patient.PatientID);
            if (pm == null) return Json(new { success = false, message = "Not found" });

            _context.PatientMedications.Remove(pm);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Medication removed!" });
        }

        // CONSENT
        public async Task<IActionResult> Consent()
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var viewModel = new PatientConsentManagementViewModel
            {
                PatientID = patient.PatientID,
                FullName = $"{patient.FirstName} {patient.LastName}",
                ActiveConsents = await _context.PatientConsentManagements
                    .Include(pcm => pcm.Doctor).ThenInclude(d => d.User)
                    .Where(pcm => pcm.PatientID == patient.PatientID && pcm.IsActive)
                    .Select(pcm => new PatientConsentViewModel
                    {
                        ConsentID = pcm.ConsentID,
                        DoctorID = pcm.DoctorID,
                        DoctorName = pcm.Doctor != null && pcm.Doctor.User != null
                            ? $"{pcm.Doctor.User.Name} {pcm.Doctor.User.Surname}" : "Unknown",
                        DoctorEmail = pcm.Doctor != null && pcm.Doctor.User != null ? pcm.Doctor.User.Email : "",
                        GrantedDate = pcm.GrantedDate,
                        IsActive = pcm.IsActive,
                        Notes = pcm.Notes
                    }).ToListAsync(),
                AvailableDoctors = await _context.Doctors.Include(d => d.User)
                    .Where(d => d.User != null && d.User.IsActive)
                    .Select(d => new DoctorViewModel
                    {
                        DoctorID = d.DoctorID,
                        FullName = d.User != null ? $"{d.User.Name} {d.User.Surname}" : "Unknown",
                        Email = d.User != null ? d.User.Email : "",
                        HPCSANumber = d.HPCSANumber,
                        Specialization = d.Specialization ?? "General"
                    }).ToListAsync(),
                AvailableTestRequests = new List<TestRequestViewModel>()
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GrantConsent(int doctorId, string notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var existing = await _context.PatientConsentManagements
                .FirstOrDefaultAsync(pcm => pcm.PatientID == patient.PatientID && pcm.DoctorID == doctorId && pcm.IsActive);
            if (existing != null) return Json(new { success = false, message = "Already have consent with this doctor" });

            _context.PatientConsentManagements.Add(new PatientConsentManagement
            {
                PatientID = patient.PatientID,
                DoctorID = doctorId,
                GrantedDate = DateTime.Now,
                IsActive = true,
                Notes = notes
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Consent granted!" });
        }

        [HttpPost]
        public async Task<IActionResult> RevokeConsent(int consentId)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var consent = await _context.PatientConsentManagements
                .FirstOrDefaultAsync(pcm => pcm.ConsentID == consentId && pcm.PatientID == patient.PatientID);
            if (consent == null) return Json(new { success = false, message = "Not found" });

            consent.IsActive = false;
            consent.RevokedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Consent revoked!" });
        }
        // ============================================
        // TRENDS (with real database data)
        // ============================================
        public async Task<IActionResult> Trends(string testType = null, int months = 6)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get available test types (from patient's test results)
            var availableTestTypes = new List<string> { "Cholesterol", "Haemoglobin", "Platelets", "WBC Count", "Glucose" };
            var selectedType = testType ?? "Cholesterol";

            // Get trend data for the selected test type
            var trendData = new List<TrendDataPoint>();

            try
            {
                // Query real data from TestResults
                var results = await _context.TestResults
                    .Include(tr => tr.RequestTestType)
                        .ThenInclude(rtt => rtt.TestType)
                    .Include(tr => tr.RequestTestType)
                        .ThenInclude(rtt => rtt.TestRequest)
                    .Where(tr => tr.RequestTestType.TestRequest.PatientID == patient.PatientID
                              && tr.RequestTestType.TestType.TestName.Contains(selectedType)
                              && tr.RequestTestType.TestRequest.RequestDate >= DateTime.Now.AddMonths(-months))
                    .OrderBy(tr => tr.RequestTestType.TestRequest.RequestDate)
                    .Select(tr => new
                    {
                        Date = tr.RequestTestType.TestRequest.RequestDate,
                        Value = tr.NumericValue ?? 0,
                        LowerLimit = tr.RequestTestType.TestType.NormalRangeMin,
                        UpperLimit = tr.RequestTestType.TestType.NormalRangeMax
                    })
                    .ToListAsync();

                foreach (var r in results)
                {
                    trendData.Add(new TrendDataPoint
                    {
                        Date = r.Date,
                        Value = Convert.ToDouble(r.Value),
                        LowerLimit = r.LowerLimit.HasValue ? Convert.ToDouble(r.LowerLimit.Value) : null,
                        UpperLimit = r.UpperLimit.HasValue ? Convert.ToDouble(r.UpperLimit.Value) : null
                    });
                }
            }
            catch
            {
                // If no data yet, return empty list
                trendData = new List<TrendDataPoint>();
            }

            var viewModel = new PatientTrendsViewModel
            {
                PatientID = patient.PatientID,
                FullName = $"{patient.FirstName} {patient.LastName}",
                AvailableTestTypes = availableTestTypes,
                SelectedTestType = selectedType,
                Months = months,
                TrendData = trendData
            };

            return View(viewModel);
        }

        // REGISTER
        public IActionResult Register()
        {
            return View(new PatientRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PatientRegistrationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered");
                return View(model);
            }

            if (await _context.Patients.AnyAsync(p => p.SAIDNumber == model.SAIDNumber))
            {
                ModelState.AddModelError("SAIDNumber", "SA ID already registered");
                return View(model);
            }

            var user = new User
            {
                Name = model.FirstName.Trim(),
                Surname = model.LastName.Trim(),
                Email = model.Email.Trim().ToLower(),
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                UserRole = "Patient",
                ContactNumber = model.Cellphone,
                IsActive = true,
                IsPasswordChanged = false,
                CreatedDate = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.Patients.Add(new Patient
            {
                UserID = user.UserID,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                SAIDNumber = model.SAIDNumber,
                DateOfBirth = model.DateOfBirth,
                Cellphone = model.Cellphone,
                HomeAddress = model.HomeAddress?.Trim(),
                Email = model.Email.Trim().ToLower(),
                IsActive = true,
                CreatedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login", "Account");
        }
    }

    public class EditPatientProfileViewModel
    {
        public int PatientID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Cellphone { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}