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

        // ============================================
        // DASHBOARD
        // ============================================
        public IActionResult Dashboard()
        {
            var viewModel = new PatientDashboardViewModel
            {
                PatientID = 1,
                FirstName = "Thabo",
                LastName = "Mbeki",
                SAIDNumber = "8001015009087",
                Email = "thabo.mbeki@email.com",
                Cellphone = "0821234567",
                DateOfBirth = new DateTime(1980, 1, 1),
                TotalTestRequests = 6,
                AbnormalResultsCount = 1,
                ReportsAvailable = 4,
                HealthScore = 82,
                HealthStatusMessage = "Stable condition with improving cholesterol levels.",
                CholesterolTrend = "6.2 → 5.8 → 5.5 → 5.2",
                RecentTestResults = new List<RecentTestResultViewModel>
                {
                    new RecentTestResultViewModel
                    {
                        TestRequestID = 1,
                        RequestDate = DateTime.Now.AddDays(-5),
                        TestName = "Full Blood Count - Haemoglobin",
                        ResultValue = "13.2 g/dL",
                        NormalRange = "12-16 g/dL",
                        Status = "Normal",
                        DoctorName = "Dr. Sarah Johnson",
                        ReportUrl = "#",
                        IsAbnormal = false
                    },
                    new RecentTestResultViewModel
                    {
                        TestRequestID = 2,
                        RequestDate = DateTime.Now.AddDays(-5),
                        TestName = "Full Blood Count - WCC",
                        ResultValue = "7.5 x10³/µL",
                        NormalRange = "4-11 x10³/µL",
                        Status = "Normal",
                        DoctorName = "Dr. Sarah Johnson",
                        ReportUrl = "#",
                        IsAbnormal = false
                    },
                    new RecentTestResultViewModel
                    {
                        TestRequestID = 3,
                        RequestDate = DateTime.Now.AddDays(-20),
                        TestName = "Lipid Profile - Cholesterol",
                        ResultValue = "5.2 mmol/L",
                        NormalRange = "<5.0 mmol/L",
                        Status = "Borderline High",
                        DoctorName = "Dr. Michael Brown",
                        ReportUrl = "#",
                        IsAbnormal = true
                    }
                },
                UpcomingTests = new List<UpcomingTestViewModel>
                {
                    new UpcomingTestViewModel { TestName = "FBC Follow-up", RequestDate = DateTime.Now.AddDays(5) },
                    new UpcomingTestViewModel { TestName = "Lipid Profile Annual Check", RequestDate = DateTime.Now.AddMonths(1) }
                }
            };

            return View(viewModel);
        }
        // ============================================
        // GET LOGGED-IN PATIENT
        // ============================================
        private async Task<Patient> GetLoggedInPatient()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return null;
            }

            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == userId.Value && p.IsActive);
        }
        // ============================================
        // PROFILE
        // ============================================
        public IActionResult Profile()
        {
            var viewModel = new PatientProfileViewModel
            {
                PatientID = 1,
                FirstName = "Thabo",
                LastName = "Mbeki",
                SAIDNumber = "8001015009087",
                Email = "thabo.mbeki@email.com",
                Cellphone = "0821234567",
                HomeAddress = "123 Soweto Street, Johannesburg",
                DateOfBirth = new DateTime(1980, 1, 1),
                Conditions = new List<PatientConditionViewModel>
                {
                    new PatientConditionViewModel
                    {
                        PatientConditionID = 1,
                        ConditionID = 1,
                        ConditionName = "Diabetes Type 2",
                        CategoryName = "Metabolic",
                        DiagnosedDate = DateTime.Now.AddYears(-5),
                        IsActive = true
                    },
                    new PatientConditionViewModel
                    {
                        PatientConditionID = 2,
                        ConditionID = 2,
                        ConditionName = "Hypertension",
                        CategoryName = "Cardiovascular",
                        DiagnosedDate = DateTime.Now.AddYears(-3),
                        IsActive = true
                    }
                },
                Allergies = new List<PatientAllergyViewModel>
                {
                    new PatientAllergyViewModel
                    {
                        PatientAllergyID = 1,
                        AllergyID = 1,
                        AllergyName = "Penicillin",
                        CategoryName = "Medication",
                        Severity = "Severe",
                        Notes = "Causes anaphylaxis"
                    }
                },
                Medications = new List<PatientMedicationViewModel>
                {
                    new PatientMedicationViewModel
                    {
                        PatientMedicationID = 1,
                        MedicationID = 1,
                        MedicationName = "Metformin",
                        CategoryName = "Diabetes",
                        Dosage = "500mg",
                        Frequency = "Twice daily",
                        StartDate = DateTime.Now.AddYears(-5),
                        IsCurrent = true
                    },
                    new PatientMedicationViewModel
                    {
                        PatientMedicationID = 2,
                        MedicationID = 2,
                        MedicationName = "Lisinopril",
                        CategoryName = "Cardiovascular",
                        Dosage = "10mg",
                        Frequency = "Once daily",
                        StartDate = DateTime.Now.AddYears(-3),
                        IsCurrent = true
                    }
                }
            };

            return View(viewModel);
        }

        // ============================================
        // EDIT PROFILE
        // ============================================
        public IActionResult EditProfile()
        {
            var viewModel = new EditPatientProfileViewModel
            {
                PatientID = 1,
                FirstName = "Thabo",
                LastName = "Mbeki",
                Cellphone = "0821234567",
                HomeAddress = "123 Soweto Street, Johannesburg",
                Email = "thabo.mbeki@email.com"
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditPatientProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // ============================================
        // MEDICAL HISTORY
        // ============================================
        public IActionResult MedicalHistory()
        {
            var viewModel = new PatientMedicalHistoryViewModel
            {
                PatientID = 1,
                FullName = "Thabo Mbeki",
                Conditions = new List<PatientConditionViewModel>
                {
                    new PatientConditionViewModel
                    {
                        PatientConditionID = 1,
                        ConditionID = 1,
                        ConditionName = "Diabetes Type 2",
                        CategoryName = "Metabolic",
                        DiagnosedDate = DateTime.Now.AddYears(-5),
                        Notes = "Controlled with medication",
                        IsActive = true
                    }
                },
                Allergies = new List<PatientAllergyViewModel>
                {
                    new PatientAllergyViewModel
                    {
                        PatientAllergyID = 1,
                        AllergyID = 1,
                        AllergyName = "Penicillin",
                        CategoryName = "Medication",
                        Severity = "Severe",
                        Notes = "Causes anaphylaxis"
                    }
                },
                Medications = new List<PatientMedicationViewModel>
                {
                    new PatientMedicationViewModel
                    {
                        PatientMedicationID = 1,
                        MedicationID = 1,
                        MedicationName = "Metformin",
                        CategoryName = "Diabetes",
                        Dosage = "500mg",
                        Frequency = "Twice daily",
                        StartDate = DateTime.Now.AddYears(-5),
                        IsCurrent = true
                    }
                },
                AvailableConditions = new List<MedicalCondition>(),
                AvailableAllergies = new List<Allergy>(),
                AvailableMedications = new List<Medication>()
            };

            return View(viewModel);
        }

        // ============================================
        // CONSENT
        // ============================================
        public IActionResult Consent()
        {
            var viewModel = new PatientConsentManagementViewModel
            {
                PatientID = 1,
                FullName = "Thabo Mbeki",
                ActiveConsents = new List<PatientConsentViewModel>(),
                AvailableDoctors = new List<DoctorViewModel>(),
                AvailableTestRequests = new List<TestRequestViewModel>()
            };

            return View(viewModel);
        }

        // ============================================
        // TRENDS
        // ============================================
        public IActionResult Trends(string testType = null, int months = 6)
        {
            var viewModel = new PatientTrendsViewModel
            {
                PatientID = 1,
                FullName = "Thabo Mbeki",
                AvailableTestTypes = new List<string> { "Cholesterol", "Haemoglobin", "Platelets", "WBC Count", "Glucose" },
                SelectedTestType = testType ?? "Cholesterol",
                Months = months,
                TrendData = new List<TrendDataPoint>
                {
                    new TrendDataPoint { Date = DateTime.Now.AddMonths(-5), Value = 5.2, LowerLimit = 3.0, UpperLimit = 6.0 },
                    new TrendDataPoint { Date = DateTime.Now.AddMonths(-4), Value = 5.5, LowerLimit = 3.0, UpperLimit = 6.0 },
                    new TrendDataPoint { Date = DateTime.Now.AddMonths(-3), Value = 5.8, LowerLimit = 3.0, UpperLimit = 6.0 },
                    new TrendDataPoint { Date = DateTime.Now.AddMonths(-2), Value = 6.1, LowerLimit = 3.0, UpperLimit = 6.0 },
                    new TrendDataPoint { Date = DateTime.Now.AddMonths(-1), Value = 5.9, LowerLimit = 3.0, UpperLimit = 6.0 },
                    new TrendDataPoint { Date = DateTime.Now, Value = 5.7, LowerLimit = 3.0, UpperLimit = 6.0 }
                }
            };

            return View(viewModel);
        }

        // ============================================
        // AJAX ENDPOINTS (Placeholder - will implement later)
        // ============================================
        [HttpPost]
        public IActionResult AddCondition(int conditionId, string diagnosedDate, string notes)
        {
            return Json(new { success = true, message = "Condition added successfully" });
        }

        [HttpPost]
        public IActionResult RemoveCondition(int patientConditionId)
        {
            return Json(new { success = true, message = "Condition removed successfully" });
        }

        [HttpPost]
        public IActionResult AddAllergy(int allergyId, string severity, string notes)
        {
            return Json(new { success = true, message = "Allergy added successfully" });
        }

        [HttpPost]
        public IActionResult RemoveAllergy(int patientAllergyId)
        {
            return Json(new { success = true, message = "Allergy removed successfully" });
        }

        [HttpPost]
        public IActionResult AddMedication(int medicationId, string dosage, string frequency, string startDate, string endDate, string notes)
        {
            return Json(new { success = true, message = "Medication added successfully" });
        }

        [HttpPost]
        public IActionResult RemoveMedication(int patientMedicationId)
        {
            return Json(new { success = true, message = "Medication removed successfully" });
        }

        [HttpPost]
        public IActionResult GrantConsent(int doctorId, string notes)
        {
            return Json(new { success = true, message = "Consent granted successfully" });
        }

        [HttpPost]
        public IActionResult RevokeConsent(int consentId)
        {
            return Json(new { success = true, message = "Consent revoked successfully" });
        }

        public IActionResult DownloadReport()
        {
            TempData["InfoMessage"] = "PDF report will be available in the full implementation.";
            return RedirectToAction("Dashboard");
        }

        // ============================================
        // PATIENT REGISTRATION
        // ============================================

        // GET: Patient/Register
        public IActionResult Register()
        {
            return View(new PatientRegistrationViewModel());
        }

        // POST: Patient/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PatientRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if email already exists
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(model);
                }

                // Check if SA ID already exists
                var saIdExists = await _context.Patients
                    .AnyAsync(p => p.SAIDNumber == model.SAIDNumber);

                if (saIdExists)
                {
                    ModelState.AddModelError("SAIDNumber", "SA ID already registered");
                    return View(model);
                }

                // Hash password
                var passwordHash = PasswordHelper.HashPassword(model.Password);

                // Create User
                var user = new User
                {
                    Name = model.FirstName.Trim(),
                    Surname = model.LastName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    PasswordHash = passwordHash,
                    UserRole = "Patient",
                    ContactNumber = model.Cellphone,
                    IsActive = true,
                    IsPasswordChanged = false,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create Patient
                var patient = new Patient
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
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }
    }

    // ============================================
    // EDIT PATIENT PROFILE VIEWMODEL
    // ============================================
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