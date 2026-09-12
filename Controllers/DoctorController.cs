// File: Controllers/DoctorController.cs
using Microsoft.AspNetCore.Mvc;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;

namespace NMB_HLabSys.Controllers
{
    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DoctorController> _logger;

        public DoctorController(
            IDoctorService doctorService,
            ICurrentUserService currentUser,
            ILogger<DoctorController> logger)
        {
            _doctorService = doctorService;
            _currentUser = currentUser;
            _logger = logger;
        }

        private int GetDoctorId()
        {
            if (!_currentUser.DoctorId.HasValue)
                throw new UnauthorizedAccessException("No doctor in context.");
            return _currentUser.DoctorId.Value;
        }

        // ═══════════════════════════════════════════════════════════
        // DASHBOARD
        // ═══════════════════════════════════════════════════════════
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var vm = await _doctorService.GetDashboardDataAsync(GetDoctorId());
                ViewBag.DoctorName = _currentUser.Email ?? "Doctor";
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard load failed");
                TempData["ErrorMessage"] = "Could not load dashboard: " + ex.Message;
                return View(new DoctorDashboardViewModel());
            }
        }

        // ═══════════════════════════════════════════════════════════
        // TEST REQUESTS
        // ═══════════════════════════════════════════════════════════
        public async Task<IActionResult> TestRequests(TestRequestFilterViewModel filter)
        {
            try
            {
                var vm = await _doctorService.GetTestRequestsAsync(
                    GetDoctorId(),
                    filter ?? new TestRequestFilterViewModel());
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading test requests");
                TempData["ErrorMessage"] = "Could not load test requests.";
                return View(new TestRequestIndexViewModel());
            }
        }

        public async Task<IActionResult> TestRequestDetails(int id)
        {
            var request = await _doctorService.GetTestRequestDetailsAsync(id);
            if (request == null) return NotFound();
            if (request.DoctorID != GetDoctorId()) return Forbid();

            ViewBag.PatientInfo = await _doctorService.GetPatientMedicalInfoAsync(request.PatientID);
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTestRequest(int id, string reason)
        {
            try
            {
                await _doctorService.CancelTestRequestAsync(id, reason);
                TempData["SuccessMessage"] = "Test request cancelled.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(TestRequests));
        }

        // ═══════════════════════════════════════════════════════════
        // CREATE TEST REQUEST
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> CreateTestRequest(int? patientId, string? saId)
        {
            var vm = await _doctorService.GetCreateTestRequestViewModelAsync(GetDoctorId(), patientId);
            ViewBag.PreselectedSAID = saId ?? string.Empty;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestRequest(CreateTestRequestViewModel model)
        {
            // Refill dropdowns on any error path
            async Task<IActionResult> ReloadViewAsync()
            {
                var refreshed = await _doctorService.GetCreateTestRequestViewModelAsync(GetDoctorId(), model.PatientID);
                model.Patients = refreshed.Patients;
                model.AvailableTests = refreshed.AvailableTests;
                return View(model);
            }

            if (!ModelState.IsValid)
                return await ReloadViewAsync();

            if (model.SelectedTestTypeIDs == null || !model.SelectedTestTypeIDs.Any())
            {
                ModelState.AddModelError("SelectedTestTypeIDs", "Please select at least one test type.");
                return await ReloadViewAsync();
            }

            try
            {
                var request = await _doctorService.CreateTestRequestAsync(model, GetDoctorId());
                TempData["SuccessMessage"] = $"Test request {request.RequestNumber} created successfully.";
                return RedirectToAction(nameof(TestRequestDetails), new { id = request.RequestID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test request");
                ModelState.AddModelError("", "Error creating request: " + ex.Message);
                return await ReloadViewAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // AJAX: Find patient by SA ID
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> FindPatientBySAID(string saId)
        {
            var patient = await _doctorService.FindPatientBySAIDAsync(saId);
            if (patient == null)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                patientId = patient.PatientID,
                fullName = patient.FullName,
                saIdNumber = patient.SAIDNumber,
                dateOfBirth = patient.DateOfBirth.ToString("yyyy-MM-dd"),
                age = patient.Age,
                gender = patient.Gender,
                cellphone = patient.Cellphone,
                email = patient.Email,
                address = patient.Address,
                conditions = patient.Conditions.Select(c => c.ConditionName).ToList(),
                allergies = patient.Allergies.Select(a => a.AllergyName).ToList(),
                medications = patient.Medications.Select(m => m.MedicationName).ToList()
            });
        }

        // ═══════════════════════════════════════════════════════════
        // PATIENTS
        // ═══════════════════════════════════════════════════════════
        public async Task<IActionResult> Patients(string? searchTerm)
        {
            try
            {
                var vm = await _doctorService.GetPatientsAsync(GetDoctorId(), searchTerm);
                ViewData["SearchTerm"] = searchTerm ?? string.Empty;
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patients");
                TempData["ErrorMessage"] = "Could not load patients.";
                ViewData["SearchTerm"] = string.Empty;
                return View(new PatientIndexViewModel());
            }
        }

        public async Task<IActionResult> PatientDetails(int id)
        {
            try
            {
                var vm = await _doctorService.GetPatientMedicalInfoAsync(id);
                return View(vm);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "PatientDetails: patient {Id} not found", id);
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction(nameof(Patients));
            }
        }

        [HttpGet]
        public async Task<IActionResult> RegisterPatient()
        {
            var vm = await _doctorService.GetRegisterPatientViewModelAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPatient(RegisterPatientViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var refreshed = await _doctorService.GetRegisterPatientViewModelAsync();
                model.AvailableConditions = refreshed.AvailableConditions;
                model.AvailableAllergies = refreshed.AvailableAllergies;
                model.AvailableMedications = refreshed.AvailableMedications;
                return View(model);
            }

            try
            {
                var patient = await _doctorService.RegisterPatientAsync(model, GetDoctorId());
                TempData["SuccessMessage"] = $"Patient {patient.FirstName} {patient.LastName} registered successfully.";
                return RedirectToAction(nameof(PatientDetails), new { id = patient.PatientID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering patient");
                ModelState.AddModelError("", ex.Message);
                var refreshed = await _doctorService.GetRegisterPatientViewModelAsync();
                model.AvailableConditions = refreshed.AvailableConditions;
                model.AvailableAllergies = refreshed.AvailableAllergies;
                model.AvailableMedications = refreshed.AvailableMedications;
                return View(model);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RESULTS
        // ═══════════════════════════════════════════════════════════
        public async Task<IActionResult> Results(ResultsFilterViewModel filter)
        {
            try
            {
                var vm = await _doctorService.GetResultsAsync(
                    GetDoctorId(),
                    filter ?? new ResultsFilterViewModel());
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading results");
                TempData["ErrorMessage"] = "Could not load results.";
                return View(new ResultsIndexViewModel());
            }
        }

        public async Task<IActionResult> ResultDetails(int id, bool release = false)
        {
            try
            {
                var vm = await _doctorService.GetResultDetailsAsync(id);
                ViewBag.OpenRelease = release;
                return View(vm);
            }
            catch (ArgumentException)
            {
                TempData["ErrorMessage"] = "Result not found.";
                return RedirectToAction(nameof(Results));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseResult(ReleaseResultsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide all required fields.";
                return RedirectToAction(nameof(Results));
            }

            var ok = await _doctorService.ReleaseResultAsync(
                model.ResultID,
                model.Note ?? string.Empty,
                model.ReleaseMethod,
                GetDoctorId());

            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? (model.ReleaseMethod == "appointment"
                    ? "Patient asked to book an appointment."
                    : "Result released to patient.")
                : "Failed to handle result.";

            return RedirectToAction(nameof(Results));
        }

        // ═══════════════════════════════════════════════════════════
        // ALERTS
        // ═══════════════════════════════════════════════════════════
        public async Task<IActionResult> Alerts(AlertsFilterViewModel filter)
        {
            try
            {
                var vm = await _doctorService.GetAlertsAsync(
                    GetDoctorId(),
                    filter ?? new AlertsFilterViewModel());
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading alerts");
                TempData["ErrorMessage"] = "Could not load alerts.";
                return View(new AlertsIndexViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AcknowledgeAlert(int id)
        {
            await _doctorService.AcknowledgeAlertAsync(id, GetDoctorId());
            return Json(new { success = true, message = "Alert acknowledged." });
        }

        // ═══════════════════════════════════════════════════════════
        // REPORTS
        // ═══════════════════════════════════════════════════════════
        public IActionResult Reports()
        {
            return View(new DoctorReportFilterViewModel
            {
                StartDate = DateTime.Now.AddMonths(-1),
                EndDate = DateTime.Now
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(DoctorReportFilterViewModel filter)
        {
            if (filter.StartDate > filter.EndDate)
            {
                TempData["ErrorMessage"] = "Start date cannot be after end date.";
                return RedirectToAction(nameof(Reports));
            }

            try
            {
                var bytes = await _doctorService.GenerateReportAsync(GetDoctorId(), filter);
                var fileName = $"NMBHDL_DoctorReport_{filter.StartDate:yyyyMMdd}_{filter.EndDate:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                TempData["ErrorMessage"] = "Failed to generate report: " + ex.Message;
                return RedirectToAction(nameof(Reports));
            }
        }
    }
}