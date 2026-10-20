using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.ViewModels.PatientViewModels;
using NMB_HLabSys.Services;
using System;
using System.Collections.Generic;
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

        // GET LOGGED-IN PATIENT
        private async Task<Patient> GetLoggedInPatient()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue) return null;

            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == userId.Value && p.IsActive);
        }

        // ============================================
        // DOWNLOAD TEST RESULTS PDF — 24 month default
        // ============================================
        public async Task<IActionResult> DownloadTestResultsPDF(DateTime? fromDate, DateTime? toDate)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return RedirectToAction("Login", "Account");

            var from = fromDate ?? DateTime.Now.AddMonths(-24);
            var to = toDate ?? DateTime.Now;

            try
            {
                // Reload patient with all related data needed for PDF
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
        // DASHBOARD — only shows RELEASED results
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

                var recentResults = await _context.TestResults
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
                    .Select(tr => new RecentTestResultViewModel
                    {
                        TestRequestID = tr.RequestTestType.RequestTestID,
                        RequestDate = tr.RequestTestType.TestRequest.RequestDate,
                        TestName = tr.RequestTestType.TestType != null ? tr.RequestTestType.TestType.TestName : "Unknown Test",
                        ResultValue = tr.ResultValue ?? "Pending",
                        NormalRange = tr.RequestTestType.TestType != null
                            && tr.RequestTestType.TestType.NormalRangeMin.HasValue
                            && tr.RequestTestType.TestType.NormalRangeMax.HasValue
                            ? $"{tr.RequestTestType.TestType.NormalRangeMin} - {tr.RequestTestType.TestType.NormalRangeMax}"
                            : "N/A",
                        Status = tr.IsAbnormal ? "Abnormal" : "Normal",
                        DoctorName = tr.RequestTestType.TestRequest.Doctor != null
                            && tr.RequestTestType.TestRequest.Doctor.User != null
                            ? $"Dr. {tr.RequestTestType.TestRequest.Doctor.User.Name} {tr.RequestTestType.TestRequest.Doctor.User.Surname}"
                            : "Unknown Doctor",
                        ReportUrl = "#",
                        IsAbnormal = tr.IsAbnormal
                    })
                    .ToListAsync();

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
            catch (Exception ex)
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
        // CONSENT — GET (loads requests for picker)
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

        // ============================================
        // GRANT CONSENT
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantConsent(int doctorId, int[] requestIds, string? notes)
        {
            var patient = await GetLoggedInPatient();
            if (patient == null) return Json(new { success = false, message = "Not authenticated" });

            if (requestIds == null || requestIds.Length == 0)
                return Json(new { success = false, message = "Please select at least one test request to share." });

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

        // ============================================
        // REVOKE CONSENT
        // ============================================
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
        // TRENDS — 24 month default range
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

            if (model.DateOfBirth.Date != expectedDob.Value.Date)
            {
                ModelState.AddModelError("DateOfBirth",
                    $"Date of birth must match the SA ID number (the ID indicates {expectedDob.Value:yyyy-MM-dd}).");
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