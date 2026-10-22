using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.ViewModels.PatientViewModels;
using NMB_HLabSys.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfReportService _pdfReportService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;

        public PatientController(
            ApplicationDbContext context,
            IPdfReportService pdfReportService,
            IPasswordHasher passwordHasher,
            IEmailService emailService)
        {
            _context = context;
            _pdfReportService = pdfReportService;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        // ─── GET LOGGED-IN PATIENT ───
        private async Task<Patient> GetLoggedInPatient()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue) return null;

            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == userId.Value && p.IsActive);
        }

        // ─── Formats "12 - 16 g/dL" using invariant culture (fixes 12,0000) ───
        private static string FormatNormalRange(decimal? min, decimal? max, string unit)
        {
            var inv = CultureInfo.InvariantCulture;
            if (!min.HasValue && !max.HasValue) return "N/A";
            if (min.HasValue && max.HasValue)
                return $"{min.Value.ToString("0.##", inv)} - {max.Value.ToString("0.##", inv)}" +
                       (string.IsNullOrWhiteSpace(unit) ? "" : $" {unit}");
            if (min.HasValue) return $"≥ {min.Value.ToString("0.##", inv)} {unit}".Trim();
            return $"≤ {max.Value.ToString("0.##", inv)} {unit}".Trim();
        }

        // ============================================
        // DOWNLOAD TEST RESULTS PDF (optionally per-request)
        // ============================================
        public async Task<IActionResult> DownloadTestResultsPDF(DateTime? fromDate, DateTime? toDate, int? requestId)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var from = fromDate ?? DateTime.Now.AddMonths(-24);
            var to = toDate ?? DateTime.Now;

            try
            {
                var patientWithHistory = await _context.Patients
                    .Include(p => p.TestRequests)
                        .ThenInclude(r => r.RequestTestTypes)
                            .ThenInclude(rt => rt.TestType)
                                .ThenInclude(t => t.Category)
                    .Include(p => p.TestRequests)
                        .ThenInclude(r => r.RequestTestTypes)
                            .ThenInclude(rt => rt.TestResults)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PatientID == patient.PatientID);

                if (patientWithHistory == null)
                {
                    TempData["ErrorMessage"] = "Could not load patient data.";
                    return RedirectToAction("Dashboard");
                }

                // If a specific request was requested, enforce ownership + release gate
                if (requestId.HasValue)
                {
                    var req = patientWithHistory.TestRequests
                        .FirstOrDefault(r => r.RequestID == requestId.Value);

                    if (req == null ||
                        (req.Status != "Released" && req.Status != "Appointment Requested"))
                        return Forbid();

                    patientWithHistory.TestRequests = new List<TestRequest> { req };
                }

                var pdfBytes = _pdfReportService.GeneratePatientTestResultsPdf(patientWithHistory, from, to);
                return File(pdfBytes, "application/pdf", $"TestResults_{patient.SAIDNumber}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating PDF: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // ============================================
        // DOWNLOAD MEDICAL HISTORY PDF
        // ============================================
        public async Task<IActionResult> DownloadMedicalHistoryPDF()
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            try
            {
                var patientWithHistory = await _context.Patients
                    .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition).ThenInclude(c => c.Category)
                    .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy).ThenInclude(a => a.Category)
                    .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication).ThenInclude(m => m.Category)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PatientID == patient.PatientID);

                if (patientWithHistory == null)
                {
                    TempData["ErrorMessage"] = "Could not load patient data.";
                    return RedirectToAction("MedicalHistory");
                }

                var pdfBytes = _pdfReportService.GeneratePatientMedicalHistoryPdf(patientWithHistory);
                return File(pdfBytes, "application/pdf", $"MedicalHistory_{patient.SAIDNumber}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating PDF: {ex.Message}";
                return RedirectToAction("MedicalHistory");
            }
        }

        // ============================================
        // ALL TEST REQUESTS (with status filter)
        // ============================================
        public async Task<IActionResult> Requests(string status = null)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var query = _context.TestRequests
                .Include(tr => tr.Doctor).ThenInclude(d => d.User)
                .Include(tr => tr.RequestTestTypes).ThenInclude(rtt => rtt.TestType).ThenInclude(t => t.Category)
                .Include(tr => tr.RequestTestTypes).ThenInclude(rtt => rtt.TestResults)
                .Where(tr => tr.PatientID == patient.PatientID)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(tr => tr.Status == status);

            var requests = await query
                .OrderByDescending(tr => tr.RequestDate)
                .ToListAsync();

            var vm = new PatientRequestsViewModel
            {
                PatientID = patient.PatientID,
                FullName = $"{patient.FirstName} {patient.LastName}",
                Requests = requests.Select(tr => new TestRequestSummaryViewModel
                {
                    TestRequestID = tr.RequestID,
                    RequestNumber = tr.RequestNumber ?? $"REQ-{tr.RequestID}",
                    RequestDate = tr.RequestDate,
                    Status = tr.Status ?? "Unknown",
                    Urgency = tr.Urgency ?? "Routine",
                    DoctorName = tr.Doctor?.User != null
                        ? $"Dr. {tr.Doctor.User.Name} {tr.Doctor.User.Surname}".Trim()
                        : "Unknown Doctor",
                    TestCount = tr.RequestTestTypes?.Count ?? 0,
                    CanViewResults = tr.Status == "Released" || tr.Status == "Appointment Requested",
                    IsCancelled = tr.Status == "Cancelled",
                    CancellationReason = tr.CancellationReason,
                    Results = (tr.RequestTestTypes ?? new List<RequestTestType>())
                        .Where(rtt => rtt.TestResults != null && rtt.TestResults.Any())
                        .SelectMany(rtt => rtt.TestResults.Select(res => new TestResultRowViewModel
                        {
                            TestResultID = res.ResultID,
                            TestName = rtt.TestType?.TestName ?? "Unknown Test",
                            CategoryName = rtt.TestType?.Category?.CategoryName ?? "",
                            ResultValue = res.ResultValue ?? "",
                            Unit = rtt.TestType?.UnitOfMeasurement ?? "",
                            NormalRangeDisplay = FormatNormalRange(
                                rtt.TestType?.NormalRangeMin,
                                rtt.TestType?.NormalRangeMax,
                                rtt.TestType?.UnitOfMeasurement),
                            IsAbnormal = res.IsAbnormal,
                            StatusLabel = res.IsAbnormal ? "Abnormal" : "Normal"
                        })).ToList()
                }).ToList()
            };

            return View(vm);
        }

        // ============================================
        // DASHBOARD
        // ============================================
        public async Task<IActionResult> Dashboard()
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            try
            {
                var totalTests = await _context.TestRequests
                    .CountAsync(tr => tr.PatientID == patient.PatientID);

                var abnormalResults = await _context.TestResults
                    .Include(tr => tr.RequestTestType)
                        .ThenInclude(rtt => rtt.TestRequest)
                    .Where(tr => tr.RequestTestType != null
                              && tr.RequestTestType.TestRequest != null
                              && tr.RequestTestType.TestRequest.PatientID == patient.PatientID
                              && tr.IsAbnormal
                              && (tr.RequestTestType.TestRequest.Status == "Released"
                                  || tr.RequestTestType.TestRequest.Status == "Appointment Requested"))
                    .CountAsync();

                var reportsAvailable = await _context.TestResults
                    .Include(tr => tr.RequestTestType)
                        .ThenInclude(rtt => rtt.TestRequest)
                    .Where(tr => tr.RequestTestType != null
                              && tr.RequestTestType.TestRequest != null
                              && tr.RequestTestType.TestRequest.PatientID == patient.PatientID
                              && (tr.RequestTestType.TestRequest.Status == "Released"
                                  || tr.RequestTestType.TestRequest.Status == "Appointment Requested"))
                    .CountAsync();

                var recentResultsRaw = await _context.TestResults
                    .Include(tr => tr.RequestTestType)
                        .ThenInclude(rtt => rtt.TestType)
                    .Include(tr => tr.RequestTestType)
                        .ThenInclude(rtt => rtt.TestRequest)
                            .ThenInclude(trq => trq.Doctor)
                                .ThenInclude(d => d.User)
                    .Where(tr => tr.RequestTestType != null
                              && tr.RequestTestType.TestRequest != null
                              && tr.RequestTestType.TestRequest.PatientID == patient.PatientID
                              && (tr.RequestTestType.TestRequest.Status == "Released"
                                  || tr.RequestTestType.TestRequest.Status == "Appointment Requested"))
                    .OrderByDescending(tr => tr.RequestTestType.TestRequest.RequestDate)
                    .Take(5)
                    .ToListAsync();

                var recentResults = recentResultsRaw.Select(tr => new RecentTestResultViewModel
                {
                    TestRequestID = tr.RequestTestType.RequestTestID,
                    RequestDate = tr.RequestTestType.TestRequest.RequestDate,
                    TestName = tr.RequestTestType.TestType != null ? tr.RequestTestType.TestType.TestName : "Unknown Test",
                    ResultValue = tr.ResultValue ?? "Pending",
                    NormalRange = tr.RequestTestType.TestType != null
                        ? FormatNormalRange(
                            tr.RequestTestType.TestType.NormalRangeMin,
                            tr.RequestTestType.TestType.NormalRangeMax,
                            tr.RequestTestType.TestType.UnitOfMeasurement)
                        : "N/A",
                    Status = tr.IsAbnormal ? "Abnormal" : "Normal",
                    DoctorName = tr.RequestTestType.TestRequest.Doctor != null
                        && tr.RequestTestType.TestRequest.Doctor.User != null
                        ? $"Dr. {tr.RequestTestType.TestRequest.Doctor.User.Name} {tr.RequestTestType.TestRequest.Doctor.User.Surname}"
                        : "Unknown Doctor",
                    ReportUrl = "#",
                    IsAbnormal = tr.IsAbnormal
                }).ToList();

                var upcomingTests = await _context.TestRequests
                    .Where(tr => tr.PatientID == patient.PatientID
                              && (tr.Status == "Submitted" || tr.Status == "Sample Received" || tr.Status == "In Progress"))
                    .OrderBy(tr => tr.RequestDate)
                    .Take(5)
                    .Select(tr => new UpcomingTestViewModel
                    {
                        TestName = tr.RequestNumber ?? "Test Request",
                        RequestDate = tr.RequestDate
                    })
                    .ToListAsync();

                var viewModel = new PatientDashboardViewModel
                {
                    PatientID = patient.PatientID,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    SAIDNumber = patient.SAIDNumber,
                    Email = patient.User?.Email ?? patient.Email,
                    Cellphone = patient.Cellphone,
                    DateOfBirth = patient.DateOfBirth,
                    TotalTestRequests = totalTests,
                    AbnormalResultsCount = abnormalResults,
                    ReportsAvailable = reportsAvailable,
                    HealthScore = CalculateHealthScore(abnormalResults, reportsAvailable),
                    HealthStatusMessage = GetHealthStatusMessage(abnormalResults, reportsAvailable),
                    CholesterolTrend = "No data yet",
                    RecentTestResults = recentResults,
                    UpcomingTests = upcomingTests
                };

                return View(viewModel);
            }
            catch
            {
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
                    HealthScore = 100,
                    HealthStatusMessage = "No test data yet",
                    CholesterolTrend = "No data yet",
                    RecentTestResults = new List<RecentTestResultViewModel>(),
                    UpcomingTests = new List<UpcomingTestViewModel>()
                };
                return View(viewModel);
            }
        }

        private int CalculateHealthScore(int abnormalCount, int totalResults)
        {
            if (totalResults == 0) return 100;
            if (abnormalCount == 0) return 100;
            var score = 100 - (abnormalCount * 100 / Math.Max(totalResults, 1));
            return Math.Max(score, 0);
        }

        private string GetHealthStatusMessage(int abnormalCount, int totalResults)
        {
            if (totalResults == 0) return "No test data available yet.";
            if (abnormalCount == 0) return "All results within normal ranges. Keep it up!";
            if (abnormalCount <= 1) return "Mostly stable — one result to watch.";
            return $"{abnormalCount} results need attention. Please consult your doctor.";
        }

        // ============================================
        // PROFILE
        // ============================================
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

        // ============================================
        // EDIT PROFILE
        // ============================================
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

            var newEmail = (model.Email ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(model);
            }

            var currentEmail = (patient.User?.Email ?? patient.Email ?? "").ToLower();
            if (newEmail != currentEmail)
            {
                var emailTaken = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == newEmail && u.UserID != patient.UserID);

                if (emailTaken)
                {
                    ModelState.AddModelError("Email", "This email is already registered to another account.");
                    return View(model);
                }
            }

            patient.FirstName = model.FirstName.Trim();
            patient.LastName = model.LastName.Trim();
            patient.Cellphone = model.Cellphone.Trim();
            patient.HomeAddress = model.HomeAddress?.Trim();
            patient.Email = newEmail;
            patient.ModifiedDate = DateTime.Now;

            if (patient.User != null)
            {
                patient.User.Name = model.FirstName.Trim();
                patient.User.Surname = model.LastName.Trim();
                patient.User.ContactNumber = model.Cellphone.Trim();
                patient.User.Email = newEmail;
                patient.User.ModifiedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserEmail", newEmail);
            HttpContext.Session.SetString("UserName", $"{patient.FirstName} {patient.LastName}");

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // ============================================
        // CHANGE PASSWORD
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction("Profile");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToAction("Profile");
            }

            // Enforce password rules: >=8, upper, number, special char
            if (newPassword.Length < 8 ||
                !Regex.IsMatch(newPassword, "[A-Z]") ||
                !Regex.IsMatch(newPassword, "[0-9]") ||
                !Regex.IsMatch(newPassword, @"[^A-Za-z0-9]"))
            {
                TempData["ErrorMessage"] = "New password must be 8+ characters with at least one uppercase letter, one number, and one special character.";
                return RedirectToAction("Profile");
            }

            if (patient.User == null)
            {
                TempData["ErrorMessage"] = "User account not found.";
                return RedirectToAction("Profile");
            }

            if (!_passwordHasher.VerifyPassword(currentPassword, patient.User.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("Profile");
            }

            patient.User.PasswordHash = _passwordHasher.HashPassword(newPassword);
            patient.User.IsPasswordChanged = true;
            patient.User.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Profile");
        }

        // ============================================
        // MEDICAL HISTORY
        // ============================================
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

        // ============================================
        // ADD / REMOVE CONDITION
        // ============================================
        [HttpPost]
        public async Task<IActionResult> AddCondition(int conditionId, DateTime? diagnosedDate, string notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var conditionExists = await _context.MedicalConditions
                .AnyAsync(c => c.ConditionID == conditionId && c.IsActive);

            if (!conditionExists)
                return Json(new { success = false, message = "Selected condition not found." });

            var exists = await _context.PatientConditions
                .AnyAsync(pc => pc.PatientID == patient.PatientID && pc.ConditionID == conditionId && pc.IsActive);
            if (exists) return Json(new { success = false, message = "Already have this condition" });

            _context.PatientConditions.Add(new PatientCondition
            {
                PatientID = patient.PatientID,
                ConditionID = conditionId,
                DiagnosedDate = diagnosedDate,
                Notes = notes ?? string.Empty,
                IsActive = true,
                CreatedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Condition added!" });
        }

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

        // ============================================
        // ADD / REMOVE ALLERGY
        // ============================================
        [HttpPost]
        public async Task<IActionResult> AddAllergy(int allergyId, string severity, string notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var allergyExists = await _context.Allergies
                .AnyAsync(a => a.AllergyID == allergyId && a.IsActive);

            if (!allergyExists)
                return Json(new { success = false, message = "Selected allergy not found." });

            var validSeverities = new[] { "Mild", "Moderate", "Severe", "Unknown" };
            if (!validSeverities.Contains(severity ?? "Unknown"))
                return Json(new { success = false, message = "Invalid severity value." });

            var exists = await _context.PatientAllergies
                .AnyAsync(pa => pa.PatientID == patient.PatientID && pa.AllergyID == allergyId);
            if (exists) return Json(new { success = false, message = "Already have this allergy" });

            _context.PatientAllergies.Add(new PatientAllergy
            {
                PatientID = patient.PatientID,
                AllergyID = allergyId,
                Severity = severity ?? string.Empty,
                Notes = notes ?? string.Empty,
                CreatedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Allergy added!" });
        }

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

        // ============================================
        // ADD / REMOVE MEDICATION
        // ============================================
        [HttpPost]
        public async Task<IActionResult> AddMedication(int medicationId, string dosage, string frequency, DateTime? startDate, DateTime? endDate, string notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var medicationExists = await _context.Medications
                .AnyAsync(m => m.MedicationID == medicationId && m.IsActive);

            if (!medicationExists)
                return Json(new { success = false, message = "Selected medication not found." });

            if (startDate.HasValue && endDate.HasValue && endDate < startDate)
                return Json(new { success = false, message = "End date cannot be before start date." });

            var exists = await _context.PatientMedications
                .AnyAsync(pm => pm.PatientID == patient.PatientID && pm.MedicationID == medicationId);
            if (exists) return Json(new { success = false, message = "Already have this medication" });

            _context.PatientMedications.Add(new PatientMedication
            {
                PatientID = patient.PatientID,
                MedicationID = medicationId,
                Dosage = dosage ?? string.Empty,
                Frequency = frequency ?? string.Empty,
                StartDate = startDate,
                EndDate = endDate,
                IsCurrent = endDate == null,
                Notes = notes ?? string.Empty,
                CreatedDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Medication added!" });
        }

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

        // ============================================
        // CONSENT
        // ============================================
        public async Task<IActionResult> Consent()
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var availableRequests = await _context.TestRequests
                .Where(tr => tr.PatientID == patient.PatientID)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new TestRequestViewModel
                {
                    TestRequestID = tr.RequestID,
                    RequestNumber = tr.RequestNumber,
                    RequestDate = tr.RequestDate,
                    Status = tr.Status
                })
                .ToListAsync();

            var viewModel = new PatientConsentManagementViewModel
            {
                PatientID = patient.PatientID,
                FullName = $"{patient.FirstName} {patient.LastName}",
                ActiveConsents = await _context.PatientConsentManagements
                    .Include(pcm => pcm.Doctor).ThenInclude(d => d.User)
                    .Include(pcm => pcm.ConsentTestRequestAccesses)
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
                        Notes = pcm.Notes,
                        SharedRequestCount = pcm.ConsentTestRequestAccesses != null
                            ? pcm.ConsentTestRequestAccesses.Count() : 0
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
                AvailableTestRequests = availableRequests
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantConsent(int doctorId, int[] requestIds, string? notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            if (requestIds == null || requestIds.Length == 0)
                return Json(new { success = false, message = "Please select at least one test request to share." });

            var doctorExists = await _context.Doctors.AnyAsync(d => d.DoctorID == doctorId);
            if (!doctorExists)
                return Json(new { success = false, message = "Selected doctor not found." });

            var existing = await _context.PatientConsentManagements
                .FirstOrDefaultAsync(pcm => pcm.PatientID == patient.PatientID && pcm.DoctorID == doctorId && pcm.IsActive);
            if (existing != null) return Json(new { success = false, message = "Already have consent with this doctor" });

            var validRequestIds = await _context.TestRequests
                .Where(tr => tr.PatientID == patient.PatientID && requestIds.Contains(tr.RequestID))
                .Select(tr => tr.RequestID)
                .ToListAsync();

            if (validRequestIds.Count != requestIds.Length)
                return Json(new { success = false, message = "One or more selected requests are invalid." });

            var consent = new PatientConsentManagement
            {
                PatientID = patient.PatientID,
                DoctorID = doctorId,
                GrantedDate = DateTime.Now,
                IsActive = true,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };
            _context.PatientConsentManagements.Add(consent);
            await _context.SaveChangesAsync();

            foreach (var reqId in validRequestIds)
            {
                _context.ConsentTestRequestAccesses.Add(new ConsentTestRequestAccess
                {
                    ConsentID = consent.ConsentID,
                    RequestID = reqId
                });
            }
            await _context.SaveChangesAsync();

            try
            {
                var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorID == doctorId);
                if (doctor?.User != null && !string.IsNullOrWhiteSpace(doctor.User.Email))
                {
                    var patientName = $"{patient.FirstName} {patient.LastName}".Trim();
                    var doctorName = $"{doctor.User.Name} {doctor.User.Surname}".Trim();

                    var body = $@"
                <html><body style='font-family: Arial, sans-serif;'>
                    <h2>Patient Consent Granted</h2>
                    <p>Dear Dr. {doctorName},</p>
                    <p>Patient <strong>{patientName}</strong> (SA ID: {patient.SAIDNumber}) has granted you access to <strong>{validRequestIds.Count}</strong> of their test requests.</p>
                    <p>You can now view these results in the system.</p>
                    <hr />
                    <p style='color: #6c757d; font-size: 12px;'>Nelson Mandela Bay Haematology Diagnostic Laboratories</p>
                </body></html>";

                    await _emailService.SendEmailAsync(
                        doctor.User.Email,
                        $"Patient Consent Granted — {patientName}",
                        body);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Consent email failed: {ex.Message}");
            }

            return Json(new { success = true, message = $"Consent granted for {validRequestIds.Count} request(s)!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeConsent(int consentId)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            var consent = await _context.PatientConsentManagements
                .Include(pcm => pcm.ConsentTestRequestAccesses)
                .FirstOrDefaultAsync(pcm => pcm.ConsentID == consentId && pcm.PatientID == patient.PatientID);
            if (consent == null) return Json(new { success = false, message = "Not found" });

            consent.IsActive = false;
            consent.RevokedDate = DateTime.Now;

            if (consent.ConsentTestRequestAccesses != null)
                _context.ConsentTestRequestAccesses.RemoveRange(consent.ConsentTestRequestAccesses);

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Consent revoked!" });
        }

        // ============================================
        // TRENDS
        // ============================================
        public async Task<IActionResult> Trends(string testType = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var from = fromDate ?? DateTime.Now.AddMonths(-24);
            var to = toDate ?? DateTime.Now;

            if (from > to)
            {
                var temp = from;
                from = to;
                to = temp;
            }

            var availableTestTypes = await _context.TestResults
                .Include(tr => tr.RequestTestType)
                    .ThenInclude(rtt => rtt.TestType)
                .Include(tr => tr.RequestTestType)
                    .ThenInclude(rtt => rtt.TestRequest)
                .Where(tr => tr.RequestTestType != null
                          && tr.RequestTestType.TestType != null
                          && tr.RequestTestType.TestRequest != null
                          && tr.RequestTestType.TestRequest.PatientID == patient.PatientID
                          && (tr.RequestTestType.TestRequest.Status == "Released"
                              || tr.RequestTestType.TestRequest.Status == "Appointment Requested"))
                .Select(tr => tr.RequestTestType.TestType.TestName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            if (!availableTestTypes.Any())
            {
                return View(new PatientTrendsViewModel
                {
                    PatientID = patient.PatientID,
                    FullName = $"{patient.FirstName} {patient.LastName}",
                    AvailableTestTypes = new List<string>(),
                    SelectedTestType = string.Empty,
                    FromDate = from,
                    ToDate = to,
                    TrendData = new List<TrendDataPoint>()
                });
            }

            var selectedType = string.IsNullOrWhiteSpace(testType) || !availableTestTypes.Contains(testType)
                ? availableTestTypes.First()
                : testType;

            var trendData = new List<TrendDataPoint>();

            try
            {
                var results = await _context.TestResults
                    .Include(tr => tr.RequestTestType)
                        .ThenInclude(rtt => rtt.TestType)
                    .Include(tr => tr.RequestTestType)
                        .ThenInclude(rtt => rtt.TestRequest)
                    .Where(tr => tr.RequestTestType != null
                              && tr.RequestTestType.TestType != null
                              && tr.RequestTestType.TestRequest != null
                              && tr.RequestTestType.TestRequest.PatientID == patient.PatientID
                              && tr.RequestTestType.TestType.TestName == selectedType
                              && tr.ResultDate >= from
                              && tr.ResultDate <= to
                              && tr.NumericValue.HasValue
                              && (tr.RequestTestType.TestRequest.Status == "Released"
                                  || tr.RequestTestType.TestRequest.Status == "Appointment Requested"))
                    .OrderBy(tr => tr.ResultDate)
                    .Select(tr => new
                    {
                        Date = tr.ResultDate,
                        Value = tr.NumericValue.Value,
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
                trendData = new List<TrendDataPoint>();
            }

            var viewModel = new PatientTrendsViewModel
            {
                PatientID = patient.PatientID,
                FullName = $"{patient.FirstName} {patient.LastName}",
                AvailableTestTypes = availableTestTypes,
                SelectedTestType = selectedType,
                FromDate = from,
                ToDate = to,
                TrendData = trendData
            };

            return View(viewModel);
        }

        // ============================================
        // REGISTER
        // ============================================
        public IActionResult Register()
        {
            return View(new PatientRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PatientRegistrationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var expectedDob = ExtractDobFromSaId(model.SAIDNumber);
            if (expectedDob == null)
            {
                ModelState.AddModelError("SAIDNumber",
                    "SA ID number is not valid — it must be 13 digits with a real date of birth.");
                return View(model);
            }

            if (!IsValidSaIdChecksum(model.SAIDNumber))
            {
                ModelState.AddModelError("SAIDNumber", "SA ID number is invalid. Please check and try again.");
                return View(model);
            }

            if (model.DateOfBirth.Date != expectedDob.Value.Date)
            {
                ModelState.AddModelError("DateOfBirth",
                    $"Date of birth must match the SA ID number (the ID indicates {expectedDob.Value:yyyy-MM-dd}).");
                return View(model);
            }

            // Password rules: >=8, upper, number, special char
            if (string.IsNullOrEmpty(model.Password) ||
                model.Password.Length < 8 ||
                !Regex.IsMatch(model.Password, "[A-Z]") ||
                !Regex.IsMatch(model.Password, "[0-9]") ||
                !Regex.IsMatch(model.Password, @"[^A-Za-z0-9]"))
            {
                ModelState.AddModelError("Password",
                    "Password must be at least 8 characters and contain one uppercase letter, one number, and one special character.");
                return View(model);
            }

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
                PasswordHash = _passwordHasher.HashPassword(model.Password),
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
                DateOfBirth = expectedDob.Value.Date,
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

        // ─── SA ID DOB extraction ───
        private DateTime? ExtractDobFromSaId(string saId)
        {
            if (string.IsNullOrWhiteSpace(saId) || !Regex.IsMatch(saId, @"^\d{13}$"))
                return null;

            int yy = int.Parse(saId.Substring(0, 2));
            int mm = int.Parse(saId.Substring(2, 2));
            int dd = int.Parse(saId.Substring(4, 2));

            int currentYY = DateTime.Now.Year % 100;
            int fullYear = (yy <= currentYY ? 2000 : 1900) + yy;

            try
            {
                return new DateTime(fullYear, mm, dd);
            }
            catch
            {
                return null;
            }
        }

        // ─── SA ID Luhn checksum validation ───
        private static bool IsValidSaIdChecksum(string saId)
        {
            if (string.IsNullOrWhiteSpace(saId) || !Regex.IsMatch(saId, @"^\d{13}$"))
                return false;

            int sum = 0;
            bool doubleDigit = false;

            for (int i = saId.Length - 2; i >= 0; i--)
            {
                int digit = saId[i] - '0';

                if (doubleDigit)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }

                sum += digit;
                doubleDigit = !doubleDigit;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            int lastDigit = saId[saId.Length - 1] - '0';

            return checkDigit == lastDigit;
        }
    }
}