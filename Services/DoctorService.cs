// File: Services/DoctorService.cs
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorService> _logger;
        private readonly IBarcodeService _barcodeService;
        private readonly IPasswordGenerator _passwordGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IPdfReportService _pdfService;

        public DoctorService(
            ApplicationDbContext context,
            ILogger<DoctorService> logger,
            IBarcodeService barcodeService,
            IPasswordGenerator passwordGenerator,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IPdfReportService pdfService)
        {
            _context = context;
            _logger = logger;
            _barcodeService = barcodeService;
            _passwordGenerator = passwordGenerator;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _pdfService = pdfService;
        }

        // ═══════════════════════════════════════════════════════════
        // DASHBOARD — OWN requests only
        // ═══════════════════════════════════════════════════════════

        public async Task<DoctorDashboardViewModel> GetDashboardDataAsync(int doctorId)
        {
            var requests = await _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestType)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestResults)
                .AsNoTracking()
                .ToListAsync();

            var activeRequests = requests.Count(r =>
                r.Status == "Submitted" ||
                r.Status == "Samples Received" ||
                r.Status == "In Progress");

            var abnormalCount = requests
                .Where(r => r.Status != "Cancelled")
                .Count(r => r.RequestTestTypes
                    .SelectMany(rt => rt.TestResults ?? new List<TestResult>())
                    .Any(t => t.IsAbnormal));

            var pendingReleaseCount = requests.Count(r =>
                r.ReleasedToPatientDate == null &&
                r.Status != "Cancelled" &&
                r.Status != "Submitted" &&
                r.Status != "Samples Received" &&
                r.Status != "In Progress");

            var totalPatients = await _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Select(r => r.PatientID)
                .Distinct()
                .CountAsync();

            return new DoctorDashboardViewModel
            {
                ActiveRequests = activeRequests,
                AbnormalAlerts = abnormalCount,
                PendingReleaseCount = pendingReleaseCount,
                TotalPatients = totalPatients,

                RecentRequests = requests
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .Select(r => new RecentRequestViewModel
                    {
                        RequestID = r.RequestID,
                        RequestNumber = r.RequestNumber,
                        PatientName = r.Patient != null
                            ? $"{r.Patient.FirstName} {r.Patient.LastName}"
                            : "Unknown",
                        RequestDate = r.RequestDate,
                        Status = r.Status,
                        Urgency = r.Urgency,
                        Progress = GetProgress(r.Status)
                    })
                    .ToList(),

                Stats = new DashboardStatsViewModel
                {
                    RequestsThisMonth = requests.Count(r =>
                        r.RequestDate.Month == DateTime.Now.Month &&
                        r.RequestDate.Year == DateTime.Now.Year),

                    CompletedThisMonth = requests.Count(r =>
                        r.Status == "Completed" &&
                        r.CompletedDate.HasValue &&
                        r.CompletedDate.Value.Month == DateTime.Now.Month),

                    AbnormalThisMonth = requests
                        .Where(r => r.Status != "Cancelled")
                        .SelectMany(r => r.RequestTestTypes)
                        .SelectMany(rt => rt.TestResults ?? new List<TestResult>())
                        .Count(t => t.IsAbnormal && t.ResultDate.Month == DateTime.Now.Month),

                    PendingThisMonth = requests.Count(r =>
                        r.ReleasedToPatientDate == null &&
                        r.Status != "Cancelled" &&
                        r.Status != "Submitted" &&
                        r.Status != "Samples Received" &&
                        r.Status != "In Progress")
                }
            };
        }

        private int GetProgress(string status)
        {
            return status switch
            {
                "Submitted" => 10,
                "Samples Received" => 25,
                "In Progress" => 50,
                "Completed" => 75,
                "Verified" => 90,
                "Appointment Requested" => 95,
                "Released" => 100,
                "Cancelled" => 0,
                _ => 0
            };
        }

        // ═══════════════════════════════════════════════════════════
        // TEST REQUESTS — OWN requests only
        // ═══════════════════════════════════════════════════════════

        public async Task<TestRequestIndexViewModel> GetTestRequestsAsync(int doctorId, TestRequestFilterViewModel filter)
        {
            filter ??= new TestRequestFilterViewModel();

            var query = _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(r => r.RequestNumber.Contains(filter.SearchTerm) ||
                    (r.Patient != null && (r.Patient.FirstName.Contains(filter.SearchTerm) ||
                                            r.Patient.LastName.Contains(filter.SearchTerm))));
            }

            if (!string.IsNullOrWhiteSpace(filter.Urgency) && filter.Urgency != "all")
                query = query.Where(r => r.Urgency == filter.Urgency);

            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "all")
            {
                if (filter.Status == "active")
                {
                    query = query.Where(r =>
                        r.Status == "Submitted" ||
                        r.Status == "Samples Received" ||
                        r.Status == "In Progress");
                }
                else
                {
                    query = query.Where(r => r.Status == filter.Status);
                }
            }

            if (filter.StartDate.HasValue)
                query = query.Where(r => r.RequestDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(r => r.RequestDate <= filter.EndDate.Value);

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .AsNoTracking()
                .ToListAsync();

            return new TestRequestIndexViewModel
            {
                Requests = requests.Select(r => new TestRequestListViewModel
                {
                    RequestID = r.RequestID,
                    RequestNumber = r.RequestNumber,
                    PatientName = r.Patient != null
                        ? $"{r.Patient.FirstName} {r.Patient.LastName}"
                        : "Unknown",
                    RequestDate = r.RequestDate,
                    Status = r.Status,
                    Urgency = r.Urgency,
                    Tests = r.RequestTestTypes
                        .Select(rt => rt.TestType != null ? rt.TestType.TestName : "Unknown")
                        .ToList(),
                    Progress = GetProgress(r.Status),
                    HasAbnormal = r.RequestTestTypes.Any(rt =>
                        rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal)),
                    HasResults = r.RequestTestTypes.Any(rt =>
                        rt.TestResults != null && rt.TestResults.Any()),
                    CanCancel = r.Status == "Submitted" || r.Status == "Samples Received"
                }).ToList(),

                Statistics = new TestRequestStatisticsViewModel
                {
                    Total = await _context.TestRequests.CountAsync(r => r.DoctorID == doctorId),
                    Submitted = await _context.TestRequests.CountAsync(r => r.DoctorID == doctorId && r.Status == "Submitted"),
                    InProgress = await _context.TestRequests.CountAsync(r => r.DoctorID == doctorId && r.Status == "In Progress"),
                    Completed = await _context.TestRequests.CountAsync(r => r.DoctorID == doctorId && r.Status == "Completed"),
                    Verified = await _context.TestRequests.CountAsync(r => r.DoctorID == doctorId && r.Status == "Verified"),
                    Released = await _context.TestRequests.CountAsync(r => r.DoctorID == doctorId && r.Status == "Released"),
                    Abnormal = await _context.TestRequests
                        .Where(r => r.DoctorID == doctorId)
                        .SelectMany(r => r.RequestTestTypes)
                        .Where(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal))
                        .CountAsync()
                },

                Filter = filter
            };
        }

        public async Task<CreateTestRequestViewModel> GetCreateTestRequestViewModelAsync(int doctorId, int? preselectedPatientId = null)
        {
            var patients = await _context.Patients
                .Where(p => p.IsActive)
                .Select(p => new PatientDropdownItem
                {
                    PatientID = p.PatientID,
                    FullName = p.FirstName + " " + p.LastName,
                    SAIDNumber = p.SAIDNumber
                })
                .OrderBy(p => p.FullName)
                .AsNoTracking()
                .ToListAsync();

            var tests = await _context.TestTypes
                .Where(t => t.IsActive)
                .Include(t => t.Category)
                .Include(t => t.SampleType)
                .Select(t => new TestTypeDropdownItem
                {
                    TestID = t.TestID,
                    TestName = t.TestName,
                    CategoryName = t.Category.CategoryName,
                    SampleTypeID = t.SampleTypeID,
                    SampleTypeName = t.SampleType.SampleTypeName,
                    TurnaroundTimeMinutes = t.TurnaroundTimeMinutes
                })
                .OrderBy(t => t.CategoryName).ThenBy(t => t.TestName)
                .AsNoTracking()
                .ToListAsync();

            return new CreateTestRequestViewModel
            {
                PatientID = preselectedPatientId ?? 0,
                Patients = patients,
                AvailableTests = tests,
                Urgency = "routine"
            };
        }

        public async Task<TestRequest> CreateTestRequestAsync(CreateTestRequestViewModel model, int doctorId)
        {
            if (model.PatientID <= 0)
                throw new InvalidOperationException("Please select a patient.");

            if (model.SelectedTestTypeIDs == null || !model.SelectedTestTypeIDs.Any())
                throw new InvalidOperationException("Please select at least one test type.");

            var patientExists = await _context.Patients.AnyAsync(p => p.PatientID == model.PatientID && p.IsActive);
            if (!patientExists)
                throw new InvalidOperationException("Selected patient not found.");

            var selectedTestIds = model.SelectedTestTypeIDs.Distinct().ToList();

            var testTypes = await _context.TestTypes
                .Include(t => t.SampleType)
                .Where(t => selectedTestIds.Contains(t.TestID) && t.IsActive)
                .ToListAsync();

            if (testTypes.Count != selectedTestIds.Count)
                throw new InvalidOperationException("One or more selected test types are invalid or inactive.");

            var sampleGroups = testTypes
                .GroupBy(t => t.SampleTypeID)
                .Select(g => new
                {
                    SampleTypeID = g.Key,
                    SampleTypeName = g.First().SampleType.SampleTypeName
                })
                .ToList();

            model.BarcodeOverrides ??= new Dictionary<int, string>();

            foreach (var sg in sampleGroups)
            {
                if (!model.BarcodeOverrides.TryGetValue(sg.SampleTypeID, out var bc) ||
                    string.IsNullOrWhiteSpace(bc))
                {
                    throw new InvalidOperationException(
                        $"Please provide a barcode for the '{sg.SampleTypeName}' sample.");
                }
            }

            var submittedBarcodes = sampleGroups
                .Select(sg => model.BarcodeOverrides[sg.SampleTypeID].Trim())
                .ToList();

            if (submittedBarcodes.Count != submittedBarcodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new InvalidOperationException("Duplicate barcodes detected. Each sample must have a unique barcode.");

            var barcodeRegex = new System.Text.RegularExpressions.Regex(@"^BAR-\d{8}-[A-Z0-9]{6}$");
            foreach (var bc in submittedBarcodes)
            {
                if (!barcodeRegex.IsMatch(bc))
                    throw new InvalidOperationException(
                        $"Barcode '{bc}' has an invalid format. Expected: BAR-YYYYMMDD-XXXXXX");
            }

            var existingBarcodes = await _context.Samples
                .Where(s => submittedBarcodes.Contains(s.Barcode))
                .Select(s => s.Barcode)
                .ToListAsync();

            if (existingBarcodes.Any())
                throw new InvalidOperationException(
                    $"These barcodes are already in use: {string.Join(", ", existingBarcodes)}");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var requestNumber = await GenerateRequestNumberAsync();

                var testRequest = new TestRequest
                {
                    RequestNumber = requestNumber,
                    DoctorID = doctorId,
                    PatientID = model.PatientID,
                    RequestDate = DateTime.Now,
                    Urgency = string.IsNullOrWhiteSpace(model.Urgency) ? "routine" : model.Urgency,
                    ClinicalNotes = model.ClinicalNotes,
                    Status = "Submitted"
                };

                _context.TestRequests.Add(testRequest);
                await _context.SaveChangesAsync();

                foreach (var testTypeId in selectedTestIds)
                {
                    _context.RequestTestTypes.Add(new RequestTestType
                    {
                        RequestID = testRequest.RequestID,
                        TestTypeID = testTypeId,
                        Status = "Submitted"
                    });
                }

                foreach (var sg in sampleGroups)
                {
                    _context.Samples.Add(new Sample
                    {
                        RequestID = testRequest.RequestID,
                        SampleTypeID = sg.SampleTypeID,
                        Barcode = model.BarcodeOverrides[sg.SampleTypeID].Trim(),
                        Status = "Pending",
                        DateReceived = null
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Test request {RequestNumber} created by doctor {DoctorId} with {SampleCount} sample(s)",
                    testRequest.RequestNumber, doctorId, sampleGroups.Count);

                try
                {
                    var patient = await _context.Patients
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PatientID == model.PatientID);

                    var doctor = await _context.Doctors
                        .Include(d => d.User)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.DoctorID == doctorId);

                    if (patient != null && !string.IsNullOrWhiteSpace(patient.Email))
                    {
                        var testNames = testTypes.Select(t => t.TestName).ToList();
                        var doctorName = doctor?.User != null
                            ? $"{doctor.User.Name} {doctor.User.Surname}".Trim()
                            : "Your Doctor";

                        await _emailService.SendTestRequestSubmittedEmailAsync(
                            toEmail: patient.Email,
                            patientName: $"{patient.FirstName} {patient.LastName}".Trim(),
                            requestNumber: testRequest.RequestNumber,
                            doctorName: doctorName,
                            requestDate: testRequest.RequestDate,
                            urgency: testRequest.Urgency,
                            testNames: testNames,
                            clinicalNotes: testRequest.ClinicalNotes ?? string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to email patient {PatientId} about new request", model.PatientID);
                }

                return testRequest;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var todayCount = await _context.TestRequests
                .CountAsync(r => r.RequestNumber.StartsWith($"REQ-{datePart}"));

            var sequence = (todayCount + 1).ToString("D4");
            return $"REQ-{datePart}-{sequence}";
        }

        public async Task<TestRequest?> GetTestRequestDetailsAsync(int requestId)
        {
            return await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.Samples).ThenInclude(s => s.SampleType)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestType).ThenInclude(t => t.Category)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestType).ThenInclude(t => t.SampleType)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestResults)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RequestID == requestId);
        }

        public async Task<bool> CancelTestRequestAsync(int requestId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
                throw new InvalidOperationException(
                    "Please provide a cancellation reason of at least 10 characters.");

            var request = await _context.TestRequests
                .Include(r => r.RequestTestTypes)
                .Include(r => r.Samples)
                .FirstOrDefaultAsync(r => r.RequestID == requestId);

            if (request == null) return false;

            if (request.Status != "Submitted" && request.Status != "Samples Received")
                throw new InvalidOperationException($"Cannot cancel request with status: {request.Status}");

            request.Status = "Cancelled";
            request.CancelledDate = DateTime.Now;
            request.CancellationReason = reason.Trim();

            foreach (var rt in request.RequestTestTypes)
            {
                if (rt.Status != "Completed" && rt.Status != "Verified")
                {
                    rt.Status = "Cancelled";
                    rt.CompletedDateTime = null;
                    rt.StartDateTime = null;
                }
            }

            foreach (var sample in request.Samples)
            {
                if (sample.Status != "Received" && sample.Status != "Processing")
                {
                    sample.Status = "Cancelled";
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTestRequestStatusAsync(int requestId, string status)
        {
            var request = await _context.TestRequests.FindAsync(requestId);
            if (request == null) return false;

            request.Status = status;
            if (status == "Completed")
                request.CompletedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        // PATIENTS — derived from OWN requests only
        // ═══════════════════════════════════════════════════════════

        public async Task<PatientIndexViewModel> GetPatientsAsync(int doctorId, string? searchTerm = null)
        {
            var patientIds = await _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Select(r => r.PatientID)
                .Distinct()
                .ToListAsync();

            var query = _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition)
                .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .Where(p => patientIds.Contains(p.PatientID));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.FirstName.Contains(searchTerm) ||
                    p.LastName.Contains(searchTerm) ||
                    p.SAIDNumber.Contains(searchTerm));
            }

            var patients = await query.AsNoTracking().ToListAsync();

            return new PatientIndexViewModel
            {
                Patients = patients.Select(p => new PatientListViewModel
                {
                    PatientID = p.PatientID,
                    FullName = $"{p.FirstName} {p.LastName}",
                    SAIDNumber = p.SAIDNumber,
                    DateOfBirth = p.DateOfBirth,
                    Cellphone = p.Cellphone,
                    Conditions = p.PatientConditions
                        .Select(pc => pc.Condition != null ? pc.Condition.ConditionName : "Unknown")
                        .ToList(),
                    Allergies = p.PatientAllergies
                        .Select(pa => pa.Allergy != null ? pa.Allergy.AllergyName : "Unknown")
                        .ToList(),
                    Medications = p.PatientMedications
                        .Select(pm => pm.Medication != null ? pm.Medication.MedicationName : "Unknown")
                        .ToList()
                }).ToList(),

                Statistics = new PatientStatisticsViewModel
                {
                    Total = patientIds.Count,
                    ActiveThisMonth = await _context.TestRequests
                        .Where(r => r.DoctorID == doctorId &&
                                    r.RequestDate.Month == DateTime.Now.Month &&
                                    r.RequestDate.Year == DateTime.Now.Year)
                        .Select(r => r.PatientID)
                        .Distinct()
                        .CountAsync(),
                    WithAbnormalResults = await _context.TestRequests
                        .Where(r => r.DoctorID == doctorId)
                        .SelectMany(r => r.RequestTestTypes)
                        .Where(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal))
                        .Select(rt => rt.RequestID)
                        .Distinct()
                        .CountAsync()
                }
            };
        }

        public async Task<Patient> RegisterPatientAsync(RegisterPatientViewModel model, int doctorId)
        {
            if (await _context.Patients.AnyAsync(p => p.SAIDNumber == model.SAIDNumber))
                throw new InvalidOperationException($"A patient with SA ID {model.SAIDNumber} already exists.");

            var resolvedEmail = string.IsNullOrWhiteSpace(model.Email)
                ? $"{model.SAIDNumber}@patient.nmblab.co.za"
                : model.Email;

            if (await _context.Users.AnyAsync(u => u.Email == resolvedEmail))
                throw new InvalidOperationException($"A user with email {resolvedEmail} already exists.");

            var contactNumber = (model.Cellphone ?? "").Trim();
            if (contactNumber.Length > 20) contactNumber = contactNumber.Substring(0, 20);

            var cellphone = (model.Cellphone ?? "").Trim();
            if (cellphone.Length > 15) cellphone = cellphone.Substring(0, 15);

            var homeAddress = string.Join(", ",
                new[] { model.AddressLine1, model.AddressLine2, model.City, model.Province, model.PostalCode }
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()));
            if (homeAddress.Length > 500) homeAddress = homeAddress.Substring(0, 500);

            var tempPassword = _passwordGenerator.GenerateTemporaryPassword();
            var hashedPassword = _passwordHasher.HashPassword(tempPassword);

            var user = new User
            {
                Name = model.Name?.Trim() ?? "",
                Surname = model.Surname?.Trim() ?? "",
                Email = resolvedEmail,
                PasswordHash = hashedPassword,
                UserRole = "Patient",
                ContactNumber = contactNumber,
                IsActive = true,
                IsPasswordChanged = false,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patient = new Patient
            {
                UserID = user.UserID,
                FirstName = model.Name?.Trim() ?? "",
                LastName = model.Surname?.Trim() ?? "",
                SAIDNumber = model.SAIDNumber,
                DateOfBirth = model.DateOfBirth,
                Cellphone = cellphone,
                Email = user.Email,
                HomeAddress = homeAddress,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            foreach (var cid in model.SelectedConditionIDs.Distinct())
            {
                _context.PatientConditions.Add(new PatientCondition
                {
                    PatientID = patient.PatientID,
                    ConditionID = cid,
                    DiagnosedDate = DateTime.Now,
                    Notes = "",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                });
            }

            foreach (var aid in model.SelectedAllergyIDs.Distinct())
            {
                _context.PatientAllergies.Add(new PatientAllergy
                {
                    PatientID = patient.PatientID,
                    AllergyID = aid,
                    Severity = "Unknown",
                    Notes = "",
                    CreatedDate = DateTime.Now
                });
            }

            foreach (var mid in model.SelectedMedicationIDs.Distinct())
            {
                _context.PatientMedications.Add(new PatientMedication
                {
                    PatientID = patient.PatientID,
                    MedicationID = mid,
                    Dosage = "",
                    Frequency = "",
                    IsCurrent = true,
                    StartDate = DateTime.Now,
                    Notes = "",
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendPatientWelcomeEmailAsync(
                    user.Email,
                    $"{model.Name} {model.Surname}".Trim(),
                    tempPassword);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to patient {Email}", user.Email);
            }

            return patient;
        }

        // ═══════════════════════════════════════════════════════════
        // PATIENT MEDICAL INFO — own ∪ consent-shared
        // (This is the ONLY method that uses the consent whitelist.)
        // ═══════════════════════════════════════════════════════════

        public async Task<PatientMedicalInfoViewModel> GetPatientMedicalInfoAsync(int patientId, int? requestingDoctorId = null)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition).ThenInclude(c => c.Category)
                .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy).ThenInclude(a => a.Category)
                .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication).ThenInclude(m => m.Category)
                .Include(p => p.TestRequests).ThenInclude(r => r.RequestTestTypes).ThenInclude(rt => rt.TestType)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            if (patient == null)
                throw new ArgumentException($"Patient with ID {patientId} not found");

            var today = DateTime.Today;
            var age = today.Year - patient.DateOfBirth.Year;
            if (patient.DateOfBirth.Date > today.AddYears(-age)) age--;

            var gender = "Unknown";
            if (!string.IsNullOrEmpty(patient.SAIDNumber) && patient.SAIDNumber.Length == 13)
            {
                var genderDigit = patient.SAIDNumber[6] - '0';
                gender = genderDigit >= 5 ? "Male" : "Female";
            }

            // ─── CONSENT FILTER ───
            // Doctors see (a) their own requests for this patient, and
            // (b) any requests the patient has explicitly shared via consent.
            List<int> allowedRequestIds = new();
            var enforceConsent = requestingDoctorId.HasValue;

            if (enforceConsent)
            {
                var consent = await _context.PatientConsentManagements
                    .Include(pcm => pcm.ConsentTestRequestAccesses)
                    .FirstOrDefaultAsync(pcm =>
                        pcm.PatientID == patientId &&
                        pcm.DoctorID == requestingDoctorId!.Value &&
                        pcm.IsActive);

                if (consent?.ConsentTestRequestAccesses != null)
                {
                    allowedRequestIds = consent.ConsentTestRequestAccesses
                        .Select(a => a.RequestID)
                        .ToList();
                }
            }

            var visibleRequests = enforceConsent
                ? patient.TestRequests.Where(r =>
                    r.DoctorID == requestingDoctorId!.Value ||
                    allowedRequestIds.Contains(r.RequestID))
                : patient.TestRequests;

            return new PatientMedicalInfoViewModel
            {
                PatientID = patient.PatientID,
                FullName = $"{patient.FirstName} {patient.LastName}",
                SAIDNumber = patient.SAIDNumber,
                DateOfBirth = patient.DateOfBirth,
                Cellphone = patient.Cellphone,
                Email = patient.Email,
                Address = patient.HomeAddress,
                Age = age,
                Gender = gender,

                Conditions = patient.PatientConditions
                    .Where(pc => pc.IsActive)
                    .Select(pc => new ConditionViewModel
                    {
                        ConditionID = pc.ConditionID,
                        ConditionName = pc.Condition != null ? pc.Condition.ConditionName : "Unknown",
                        Category = pc.Condition?.Category?.CategoryName ?? "Unknown",
                        DiagnosedDate = pc.DiagnosedDate
                    }).ToList(),

                Allergies = patient.PatientAllergies.Select(pa => new AllergyViewModel
                {
                    AllergyID = pa.AllergyID,
                    AllergyName = pa.Allergy != null ? pa.Allergy.AllergyName : "Unknown",
                    Category = pa.Allergy?.Category?.CategoryName ?? "Unknown",
                    Severity = pa.Severity
                }).ToList(),

                Medications = patient.PatientMedications
                    .Where(pm => pm.IsCurrent)
                    .Select(pm => new MedicationViewModel
                    {
                        MedicationID = pm.MedicationID,
                        MedicationName = pm.Medication != null ? pm.Medication.MedicationName : "Unknown",
                        Category = pm.Medication?.Category?.CategoryName ?? "Unknown",
                        Dosage = pm.Dosage,
                        IsCurrent = pm.IsCurrent
                    }).ToList(),

                RecentRequests = visibleRequests
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .Select(r => new TestRequestSummaryViewModel
                    {
                        RequestID = r.RequestID,
                        RequestNumber = r.RequestNumber,
                        RequestDate = r.RequestDate,
                        Status = r.Status,
                        Tests = r.RequestTestTypes
                            .Select(rt => rt.TestType != null ? rt.TestType.TestName : "Unknown")
                            .ToList()
                    }).ToList()
            };
        }

        public async Task<PatientMedicalInfoViewModel?> FindPatientBySAIDAsync(string saIdNumber)
        {
            if (string.IsNullOrWhiteSpace(saIdNumber)) return null;

            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SAIDNumber == saIdNumber && p.IsActive);

            if (patient == null) return null;

            return await GetPatientMedicalInfoAsync(patient.PatientID);
        }

        public async Task<RegisterPatientViewModel> GetRegisterPatientViewModelAsync()
        {
            return new RegisterPatientViewModel
            {
                AvailableConditions = await _context.MedicalConditions
                    .Where(c => c.IsActive)
                    .Select(c => new LookupItem { Id = c.ConditionID, Name = c.ConditionName })
                    .OrderBy(c => c.Name)
                    .AsNoTracking()
                    .ToListAsync(),

                AvailableAllergies = await _context.Allergies
                    .Where(a => a.IsActive)
                    .Select(a => new LookupItem { Id = a.AllergyID, Name = a.AllergyName })
                    .OrderBy(a => a.Name)
                    .AsNoTracking()
                    .ToListAsync(),

                AvailableMedications = await _context.Medications
                    .Where(m => m.IsActive)
                    .Select(m => new LookupItem { Id = m.MedicationID, Name = m.MedicationName })
                    .OrderBy(m => m.Name)
                    .AsNoTracking()
                    .ToListAsync()
            };
        }

        // ═══════════════════════════════════════════════════════════
        // RESULTS — OWN requests only
        // ═══════════════════════════════════════════════════════════

        public async Task<ResultsIndexViewModel> GetResultsAsync(int doctorId, ResultsFilterViewModel filter)
        {
            filter ??= new ResultsFilterViewModel();

            var query = _context.TestRequests
                .Where(r => r.DoctorID == doctorId &&
                            (r.Status == "Verified" ||
                             r.Status == "Completed" ||
                             r.Status == "Released" ||
                             r.Status == "Appointment Requested"))
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestType)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestResults)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(r => r.RequestNumber.Contains(filter.SearchTerm) ||
                    (r.Patient != null && (r.Patient.FirstName.Contains(filter.SearchTerm) ||
                                            r.Patient.LastName.Contains(filter.SearchTerm))));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status == "pending")
                query = query.Where(r => r.ReleasedToPatientDate == null);

            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status == "released")
                query = query.Where(r => r.ReleasedToPatientDate != null);

            if (!string.IsNullOrWhiteSpace(filter.AbnormalFilter) && filter.AbnormalFilter == "abnormal")
                query = query.Where(r => r.RequestTestTypes.Any(rt =>
                    rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal)));

            var results = await query
                .OrderByDescending(r => r.RequestDate)
                .AsNoTracking()
                .ToListAsync();

            var baseRequests = _context.TestRequests
                .Where(r => r.DoctorID == doctorId && r.Status != "Cancelled");

            var totalRequests = await baseRequests.CountAsync();
            var released = await baseRequests.CountAsync(r => r.ReleasedToPatientDate != null);
            var appointmentRequested = await baseRequests
                .CountAsync(r => r.ReleasedToPatientDate == null && r.AppointmentRequestedDate != null);
            var pendingRelease = await baseRequests
                .CountAsync(r => r.ReleasedToPatientDate == null && r.AppointmentRequestedDate == null);

            var totalResults = await _context.TestResults
                .Where(r => r.RequestTestType != null &&
                            r.RequestTestType.TestRequest != null &&
                            r.RequestTestType.TestRequest.DoctorID == doctorId)
                .CountAsync();

            var abnormalResults = await _context.TestResults
                .Where(r => r.IsAbnormal &&
                            r.RequestTestType != null &&
                            r.RequestTestType.TestRequest != null &&
                            r.RequestTestType.TestRequest.DoctorID == doctorId)
                .CountAsync();

            return new ResultsIndexViewModel
            {
                Results = results.Select(r => new ResultGroupViewModel
                {
                    RequestID = r.RequestID,
                    RequestNumber = r.RequestNumber,
                    PatientName = r.Patient != null
                        ? $"{r.Patient.FirstName} {r.Patient.LastName}"
                        : "Unknown",
                    RequestDate = r.RequestDate,
                    RequestStatus = r.Status,
                    ReleasedToPatientDate = r.ReleasedToPatientDate,
                    AppointmentRequestedDate = r.AppointmentRequestedDate,

                    TestResults = r.RequestTestTypes
                        .Where(rt => rt.TestResults != null && rt.TestResults.Any())
                        .SelectMany(rt => rt.TestResults.Select(t => new ResultItemViewModel
                        {
                            ResultID = t.ResultID,
                            RequestID = r.RequestID,
                            TestName = rt.TestType != null ? rt.TestType.TestName : "Unknown",
                            ResultValue = t.ResultValue ?? "N/A",
                            NormalRange = rt.TestType != null
                                ? $"{rt.TestType.NormalRangeMin} - {rt.TestType.NormalRangeMax}"
                                : "N/A",
                            UnitOfMeasurement = rt.TestType?.UnitOfMeasurement ?? string.Empty,
                            IsAbnormal = t.IsAbnormal,
                            IsReleased = r.ReleasedToPatientDate.HasValue,
                            ReleasedDate = r.ReleasedToPatientDate,
                            AppointmentRequestedDate = r.AppointmentRequestedDate,
                            ResultDate = t.ResultDate,
                            Notes = t.Notes
                        })).ToList()
                }).ToList(),

                Statistics = new ResultsStatisticsViewModel
                {
                    TotalResults = totalResults,
                    AbnormalResults = abnormalResults,
                    TotalRequests = totalRequests,
                    PendingRelease = pendingRelease,
                    AppointmentRequested = appointmentRequested,
                    Released = released
                }
            };
        }

        public async Task<ResultItemViewModel> GetResultDetailsAsync(int resultId)
        {
            var result = await _context.TestResults
                .Include(r => r.RequestTestType).ThenInclude(rt => rt.TestType)
                .Include(r => r.RequestTestType).ThenInclude(rt => rt.TestRequest).ThenInclude(tr => tr.Patient)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResultID == resultId);

            if (result == null)
                throw new ArgumentException($"Result with ID {resultId} not found");

            var rt = result.RequestTestType;
            var tr = rt?.TestRequest;
            var tt = rt?.TestType;

            return new ResultItemViewModel
            {
                ResultID = result.ResultID,
                RequestID = tr?.RequestID ?? 0,
                TestName = tt?.TestName ?? "Unknown",
                ResultValue = result.ResultValue ?? "N/A",
                NormalRange = tt != null
                    ? $"{tt.NormalRangeMin} - {tt.NormalRangeMax}"
                    : "N/A",
                UnitOfMeasurement = tt?.UnitOfMeasurement ?? string.Empty,
                IsAbnormal = result.IsAbnormal,
                IsReleased = tr?.ReleasedToPatientDate.HasValue ?? false,
                ReleasedDate = tr?.ReleasedToPatientDate,
                ReleaseNote = tr?.ReleaseNote,
                ReleaseMethod = tr?.ReleaseMethod,
                AppointmentRequestedDate = tr?.AppointmentRequestedDate,
                ResultDate = result.ResultDate,
                Notes = result.Notes,
                Interpretation = result.IsAbnormal
                    ? "This result is outside the normal reference range. Clinical correlation recommended."
                    : "Within normal reference range.",
                PatientID = tr?.PatientID ?? 0,
                PatientName = tr?.Patient != null
                    ? $"{tr.Patient.FirstName} {tr.Patient.LastName}"
                    : "Unknown",
                SAIDNumber = tr?.Patient?.SAIDNumber ?? string.Empty,
                RequestNumber = tr?.RequestNumber ?? string.Empty
            };
        }

        public async Task<bool> ReleaseResultAsync(int resultId, string note, string releaseMethod, int doctorId)
        {
            var result = await _context.TestResults
                .Include(r => r.RequestTestType)
                    .ThenInclude(rt => rt!.TestType)
                .Include(r => r.RequestTestType)
                    .ThenInclude(rt => rt!.TestRequest)
                        .ThenInclude(tr => tr!.Patient)
                .Include(r => r.RequestTestType)
                    .ThenInclude(rt => rt!.TestRequest)
                        .ThenInclude(tr => tr!.Doctor)
                            .ThenInclude(d => d!.User)
                .Include(r => r.RequestTestType)
                    .ThenInclude(rt => rt!.TestRequest)
                        .ThenInclude(tr => tr!.RequestTestTypes)
                            .ThenInclude(rt2 => rt2.TestType)
                .Include(r => r.RequestTestType)
                    .ThenInclude(rt => rt!.TestRequest)
                        .ThenInclude(tr => tr!.RequestTestTypes)
                            .ThenInclude(rt2 => rt2.TestResults)
                .FirstOrDefaultAsync(r => r.ResultID == resultId);

            if (result?.RequestTestType?.TestRequest == null) return false;

            var request = result.RequestTestType.TestRequest;

            // Owner only — consent grants view-only, not release.
            if (request.DoctorID != doctorId) return false;

            var alreadyReleased = request.ReleasedToPatientDate.HasValue;
            var wasAppointmentRequested = request.AppointmentRequestedDate.HasValue;

            request.ReleaseNote = note;
            request.ReleaseMethod = releaseMethod;

            if (releaseMethod == "appointment")
            {
                request.Status = "Appointment Requested";
                request.AppointmentRequestedDate = DateTime.Now;
            }
            else
            {
                request.Status = "Released";
                request.ReleasedToPatientDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            var shouldSendEmail =
                (releaseMethod == "appointment" && !wasAppointmentRequested) ||
                (releaseMethod != "appointment" && !alreadyReleased);

            if (shouldSendEmail)
            {
                try
                {
                    var patient = request.Patient;
                    if (patient != null && !string.IsNullOrWhiteSpace(patient.Email))
                    {
                        var patientName = $"{patient.FirstName} {patient.LastName}".Trim();
                        var doctorName = request.Doctor?.User != null
                            ? $"{request.Doctor.User.Name} {request.Doctor.User.Surname}".Trim()
                            : "Your Doctor";

                        if (releaseMethod == "appointment")
                        {
                            await _emailService.SendAppointmentRequestEmailAsync(
                                toEmail: patient.Email,
                                patientName: patientName,
                                doctorName: doctorName,
                                requestNumber: request.RequestNumber,
                                note: note ?? string.Empty);
                        }
                        else
                        {
                            var emailResults = request.RequestTestTypes?
                                .SelectMany(rt => rt.TestResults ?? new List<TestResult>())
                                .Select(t =>
                                {
                                    var tt = request.RequestTestTypes?
                                        .FirstOrDefault(rt => rt.RequestTestID == t.RequestTestID)?
                                        .TestType;

                                    return new ReleasedTestEmailItem
                                    {
                                        TestName = tt?.TestName ?? "Unknown",
                                        ResultValue = t.ResultValue ?? "N/A",
                                        Unit = tt?.UnitOfMeasurement ?? "",
                                        NormalRange = tt != null
                                            ? $"{tt.NormalRangeMin} - {tt.NormalRangeMax}"
                                            : "—",
                                        IsAbnormal = t.IsAbnormal
                                    };
                                })
                                .ToList() ?? new List<ReleasedTestEmailItem>();

                            await _emailService.SendResultReleasedEmailAsync(
                                toEmail: patient.Email,
                                patientName: patientName,
                                doctorName: doctorName,
                                requestNumber: request.RequestNumber,
                                note: note ?? string.Empty,
                                testResults: emailResults);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to email patient for released result {ResultId}", resultId);
                }
            }

            return true;
        }

        // ═══════════════════════════════════════════════════════════
        // ALERTS — OWN requests only
        // ═══════════════════════════════════════════════════════════

        public async Task<AlertsIndexViewModel> GetAlertsAsync(int doctorId, AlertsFilterViewModel filter)
        {
            filter ??= new AlertsFilterViewModel();

            var startDate = filter.StartDate ?? DateTime.Now.AddDays(-5);
            var endDate = filter.EndDate ?? DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            var query = _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Where(r => r.RequestDate >= startDate && r.RequestDate <= endDate)
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestType)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestResults)
                .Where(r => r.RequestTestTypes.Any(rt =>
                    rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal)))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(r => r.RequestNumber.Contains(filter.SearchTerm) ||
                    (r.Patient != null &&
                     (r.Patient.FirstName.Contains(filter.SearchTerm) ||
                      r.Patient.LastName.Contains(filter.SearchTerm))));
            }

            if (!string.IsNullOrWhiteSpace(filter.Urgency) && filter.Urgency != "all")
                query = query.Where(r => r.Urgency == filter.Urgency);

            var alerts = await query
                .OrderByDescending(r => r.RequestDate)
                .AsNoTracking()
                .ToListAsync();

            return new AlertsIndexViewModel
            {
                Alerts = alerts.Select(r => new AlertGroupViewModel
                {
                    RequestID = r.RequestID,
                    RequestNumber = r.RequestNumber,
                    PatientName = r.Patient != null
                        ? $"{r.Patient.FirstName} {r.Patient.LastName}"
                        : "Unknown",
                    AlertDate = r.RequestDate,
                    Urgency = r.Urgency,
                    AbnormalTests = r.RequestTestTypes
                        .Where(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal))
                        .SelectMany(rt => rt.TestResults
                            .Where(t => t.IsAbnormal)
                            .Select(t => new AbnormalTestViewModel
                            {
                                TestName = rt.TestType != null ? rt.TestType.TestName : "Unknown",
                                Result = t.ResultValue ?? "N/A",
                                NormalRange = rt.TestType != null
                                    ? $"{rt.TestType.NormalRangeMin} - {rt.TestType.NormalRangeMax}"
                                    : "N/A",
                                Severity = DetermineSeverity(r.Urgency, t.NumericValue,
                                    rt.TestType?.NormalRangeMin, rt.TestType?.NormalRangeMax),
                                ClinicalSignificance = "Clinical correlation recommended."
                            }))
                        .ToList()
                }).ToList(),

                Statistics = new AlertStatisticsViewModel
                {
                    TotalAlerts = alerts.Count,
                    CriticalCount = alerts.Count(r => r.Urgency == "stat"),
                    UrgentCount = alerts.Count(r => r.Urgency == "urgent")
                },

                Filter = new AlertsFilterViewModel
                {
                    SearchTerm = filter.SearchTerm,
                    Urgency = filter.Urgency,
                    StartDate = startDate,
                    EndDate = endDate
                }
            };
        }

        private static string DetermineSeverity(string urgency, decimal? value,
            decimal? normalMin, decimal? normalMax)
        {
            if (urgency == "stat") return "Critical";

            if (value.HasValue && normalMin.HasValue && value.Value < normalMin.Value * 0.5m)
                return "Critical";
            if (value.HasValue && normalMax.HasValue && value.Value > normalMax.Value * 2m)
                return "Critical";

            if (urgency == "urgent") return "Elevated";
            return "Abnormal";
        }

        public async Task<bool> AcknowledgeAlertAsync(int alertId, int doctorId)
        {
            _logger.LogInformation("Alert {AlertId} acknowledged by doctor {DoctorId}", alertId, doctorId);
            await Task.CompletedTask;
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        // REPORTS — OWN requests only
        // ═══════════════════════════════════════════════════════════

        public async Task<byte[]> GenerateReportAsync(int doctorId, DoctorReportFilterViewModel filter)
        {
            filter ??= new DoctorReportFilterViewModel();

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorID == doctorId);

            var startDate = filter.StartDate.Date;
            var endExclusive = filter.EndDate.Date.AddDays(1);

            var query = _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Where(r => r.RequestDate >= startDate && r.RequestDate < endExclusive)
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestType).ThenInclude(t => t.Category)
                .Include(r => r.RequestTestTypes).ThenInclude(rt => rt.TestResults)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "all")
                query = query.Where(r => r.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.Urgency) && filter.Urgency != "all")
                query = query.Where(r => r.Urgency == filter.Urgency);

            var requests = await query
                .OrderByDescending(r => r.RequestDate)
                .AsNoTracking()
                .ToListAsync();

            var reportData = new DoctorReportData
            {
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                DoctorName = doctor?.User != null
                    ? $"{doctor.User.Name} {doctor.User.Surname}".Trim()
                    : "Unknown Doctor",
                DoctorEmail = doctor?.User?.Email ?? string.Empty,
                TotalRequests = requests.Count,
                TotalTests = requests.SelectMany(r => r.RequestTestTypes).Count(),
                AbnormalTests = requests
                    .SelectMany(r => r.RequestTestTypes)
                    .SelectMany(rt => rt.TestResults ?? new List<TestResult>())
                    .Count(t => t.IsAbnormal),
                PendingTests = requests
                    .SelectMany(r => r.RequestTestTypes)
                    .Count(rt => rt.Status != "Completed" && rt.Status != "Verified"),

                Requests = requests.Select(r => new DoctorReportRequest
                {
                    RequestNumber = r.RequestNumber,
                    RequestDate = r.RequestDate,
                    PatientName = r.Patient != null
                        ? $"{r.Patient.FirstName} {r.Patient.LastName}"
                        : "Unknown",
                    SAIDNumber = r.Patient?.SAIDNumber ?? "—",
                    Urgency = r.Urgency,
                    Status = r.Status,
                    ClinicalNotes = r.ClinicalNotes ?? string.Empty,

                    Tests = r.RequestTestTypes.Select(rt =>
                    {
                        var latestResult = rt.TestResults?
                            .OrderByDescending(t => t.ResultDate)
                            .FirstOrDefault();

                        return new DoctorReportTest
                        {
                            TestName = rt.TestType?.TestName ?? "Unknown",
                            Category = rt.TestType?.Category?.CategoryName ?? "—",
                            Status = rt.Status,
                            ResultValue = latestResult?.ResultValue,
                            NormalRange = rt.TestType != null
                                ? $"{rt.TestType.NormalRangeMin} - {rt.TestType.NormalRangeMax}"
                                : null,
                            Unit = rt.TestType?.UnitOfMeasurement,
                            IsAbnormal = latestResult?.IsAbnormal ?? false,
                            CompletedDate = rt.CompletedDateTime
                        };
                    }).ToList()
                }).ToList()
            };

            return _pdfService.GenerateDoctorReportPdf(reportData);
        }
    }
}