using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;

namespace NMB_HLabSys.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorService> _logger;
        private readonly IBarcodeService _barcodeService;

        public DoctorService(
            ApplicationDbContext context,
            ILogger<DoctorService> logger,
            IBarcodeService barcodeService)
        {
            _context = context;
            _logger = logger;
            _barcodeService = barcodeService;
        }

       
        // DASHBOARD
      
        public async Task<DoctorDashboardViewModel> GetDashboardDataAsync(int doctorId)
        {
            var requests = await _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestType)
                .ToListAsync();

            var activeRequests = requests.Where(r => r.Status != "Completed" && r.Status != "Verified" && r.Status != "Released").Count();
            var abnormalCount = requests
                .SelectMany(r => r.RequestTestTypes)
                .Count(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal));

            return new DoctorDashboardViewModel
            {
                ActiveRequests = activeRequests,
                AbnormalAlerts = abnormalCount,
                PendingReleaseCount = requests.Count(r => r.Status == "Verified"),
                TotalPatients = await _context.Patients.CountAsync(p => p.UserID == doctorId),
                RecentRequests = requests.OrderByDescending(r => r.RequestDate).Take(5).Select(r => new RecentRequestViewModel
                {
                    RequestID = r.RequestID,
                    RequestNumber = r.RequestNumber,
                    PatientName = r.Patient != null ? $"{r.Patient.FirstName} {r.Patient.LastName}" : "Unknown",
                    RequestDate = r.RequestDate,
                    Status = r.Status,
                    Urgency = r.Urgency,
                    Progress = GetProgress(r.Status)
                }).ToList(),
                Stats = new DashboardStatsViewModel
                {
                    RequestsThisMonth = requests.Count(r => r.RequestDate.Month == DateTime.Now.Month && r.RequestDate.Year == DateTime.Now.Year),
                    CompletedThisMonth = requests.Count(r => r.Status == "Completed" && r.CompletedDate.HasValue && r.CompletedDate.Value.Month == DateTime.Now.Month),
                    AbnormalThisMonth = requests.SelectMany(r => r.RequestTestTypes).Count(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal && t.ResultDate.Month == DateTime.Now.Month)),
                    PendingThisMonth = requests.Count(r => r.Status == "Verified" && r.CompletedDate.HasValue && r.CompletedDate.Value.Month == DateTime.Now.Month)
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
                "Released" => 100,
                _ => 0
            };
        }

        
        // TEST REQUESTS
       
        public async Task<TestRequestIndexViewModel> GetTestRequestsAsync(int doctorId, TestRequestFilterViewModel filter)
        {
            var query = _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(r => r.RequestNumber.Contains(filter.SearchTerm) ||
                    (r.Patient != null && (r.Patient.FirstName.Contains(filter.SearchTerm) || r.Patient.LastName.Contains(filter.SearchTerm))));
            }

            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "all")
            {
                query = query.Where(r => r.Status == filter.Status);
            }

            if (!string.IsNullOrEmpty(filter.Urgency) && filter.Urgency != "all")
            {
                query = query.Where(r => r.Urgency == filter.Urgency);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(r => r.RequestDate <= filter.EndDate.Value);
            }

            var requests = await query.OrderByDescending(r => r.RequestDate).ToListAsync();

            return new TestRequestIndexViewModel
            {
                Requests = requests.Select(r => new TestRequestListViewModel
                {
                    RequestID = r.RequestID,
                    RequestNumber = r.RequestNumber,
                    PatientName = r.Patient != null ? $"{r.Patient.FirstName}   {r.Patient.LastName}" : "Unknown",
                    RequestDate = r.RequestDate,
                    Status = r.Status,
                    Urgency = r.Urgency,
                    Tests = r.RequestTestTypes.Select(rt => rt.TestType != null ? rt.TestType.TestName : "Unknown").ToList(),
                    Progress = GetProgress(r.Status),
                    HasAbnormal = r.RequestTestTypes.Any(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal)),
                    HasResults = r.RequestTestTypes.Any(rt => rt.TestResults != null && rt.TestResults.Any()),
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

        public async Task<TestRequest> CreateTestRequestAsync(CreateTestRequestViewModel model, int doctorId)
        {
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
                    Urgency = model.Urgency,
                    ClinicalNotes = model.ClinicalNotes,
                    Status = "Submitted"
                };

                _context.TestRequests.Add(testRequest);
                await _context.SaveChangesAsync();

                foreach (var testTypeId in model.SelectedTestTypeIDs)
                {
                    var requestTestType = new RequestTestType
                    {
                        RequestID = testRequest.RequestID,
                        TestTypeID = testTypeId,
                        Status = "Submitted"
                    };
                    _context.RequestTestTypes.Add(requestTestType);
                }

                var testTypes = await _context.TestTypes
                    .Include(t => t.SampleType)
                    .Where(t => model.SelectedTestTypeIDs.Contains(t.TestID))
                    .ToListAsync();

                var barcodes = _barcodeService.GenerateBarcodes(testTypes.Count);

                for (int i = 0; i < testTypes.Count; i++)
                {
                    var testType = testTypes[i];
                    var sample = new Sample
                    {
                        RequestID = testRequest.RequestID,

                        SampleTypeID = testType.SampleTypeID,
                        Barcode = barcodes[i],
                        Status = "Pending"
                    };
                    _context.Samples.Add(sample);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return testRequest;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating test request");
                throw;
            }
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var existing = await _context.TestRequests
                .Where(r => r.RequestNumber.StartsWith($"REQ-{datePart}"))
                .CountAsync();
            var sequence = (existing + 1).ToString("D4");
            return $"REQ-{datePart}-{sequence}";
        }

        public async Task<TestRequest> GetTestRequestDetailsAsync(int requestId)
        {
            return await _context.TestRequests
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestType)
                .Include(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestResults)
                .FirstOrDefaultAsync(r => r.RequestID == requestId);
        }

        public async Task<bool> CancelTestRequestAsync(int requestId, string reason)
        {
            var request = await _context.TestRequests.FindAsync(requestId);
            if (request == null) return false;

            if (request.Status != "Submitted" && request.Status != "Samples Received")
                throw new InvalidOperationException($"Cannot cancel request with status: {request.Status}");

            request.Status = "Cancelled";
            request.CancelledDate = DateTime.Now;
            request.CancellationReason = reason;

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

        
        // PATIENTS
        
        public async Task<PatientIndexViewModel> GetPatientsAsync(int doctorId, string? searchTerm = null)
        {
            var query = _context.Patients
                .Include(p => p.PatientConditions)
                .ThenInclude(pc => pc.Condition)
                .Include(p => p.PatientAllergies)
                .ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications)
                .ThenInclude(pm => pm.Medication)
                .Include(p => p.TestRequests)
                .Where(p => p.UserID == doctorId);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.FirstName.Contains(searchTerm) ||
                    p.LastName.Contains(searchTerm) ||
                    p.SAIDNumber.Contains(searchTerm));
            }

            var patients = await query.ToListAsync();

            return new PatientIndexViewModel
            {
                Patients = patients.Select(p => new PatientListViewModel
                {
                    PatientID = p.PatientID,
                    FullName = $"{p.FirstName}{p.LastName}",
                    SAIDNumber = p.SAIDNumber,
                    DateOfBirth = p.DateOfBirth,
                    Cellphone = p.Cellphone,
                    Conditions = p.PatientConditions.Select(pc => pc.Condition != null ? pc.Condition.ConditionName : "Unknown").ToList(),
                    Allergies = p.PatientAllergies.Select(pa => pa.Allergy != null ? pa.Allergy.AllergyName : "Unknown").ToList(),
                    Medications = p.PatientMedications.Select(pm => pm.Medication != null ? pm.Medication.MedicationName : "Unknown").ToList()
                }).ToList(),
                Statistics = new PatientStatisticsViewModel
                {
                    Total = await _context.Patients.CountAsync(p => p.UserID == doctorId),
                    ActiveThisMonth = await _context.TestRequests
                        .Where(r => r.DoctorID == doctorId && r.RequestDate.Month == DateTime.Now.Month)
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
            var patient = new Patient
            {
                UserID = doctorId,
                FirstName = model.Name,
                LastName = model.Surname,
                SAIDNumber = model.SAIDNumber,
                DateOfBirth = model.DateOfBirth,
                Cellphone = model.Cellphone,
                Email = model.Email,
                HomeAddress = model.Address,
                CreatedDate = DateTime.Now
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            foreach (var conditionId in model.SelectedConditionIDs)
            {
                _context.PatientConditions.Add(new PatientCondition
                {
                    PatientID = patient.PatientID,
                    ConditionID = conditionId,
                    DiagnosedDate = DateTime.Now
                });
            }

            foreach (var allergyId in model.SelectedAllergyIDs)
            {
                _context.PatientAllergies.Add(new PatientAllergy
                {
                    PatientID = patient.PatientID,
                    AllergyID = allergyId
                });
            }

            foreach (var medicationId in model.SelectedMedicationIDs)
            {
                _context.PatientMedications.Add(new PatientMedication
                {
                    PatientID = patient.PatientID,
                    MedicationID = medicationId,
                    IsCurrent = true,
                    StartDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<PatientMedicalInfoViewModel> GetPatientMedicalInfoAsync(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientConditions)
                .ThenInclude(pc => pc.Condition)
                .Include(p => p.PatientAllergies)
                .ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications)
                .ThenInclude(pm => pm.Medication)
                .Include(p => p.TestRequests)
                .ThenInclude(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestType)
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            if (patient == null)
                throw new ArgumentException($"Patient with ID {patientId} not found");

            return new PatientMedicalInfoViewModel
            {
                PatientID = patient.PatientID,
                FullName = $"{patient.FirstName}{patient.LastName}",
                SAIDNumber = patient.SAIDNumber,
                DateOfBirth = patient.DateOfBirth,
                Cellphone = patient.Cellphone,
                Email = patient.Email,
                Address = patient.HomeAddress,
                Conditions = patient.PatientConditions
                    .Where(pc => pc.IsActive)
                    .Select(pc => new ConditionViewModel
                    {
                        ConditionID = pc.ConditionID,
                        ConditionName = pc.Condition != null ? pc.Condition.ConditionName : "Unknown",
                        Category = pc.Condition != null ? pc.Condition.Category?.CategoryName ?? "Unknown" : "Unknown",
                        DiagnosedDate = pc.DiagnosedDate
                    }).ToList(),
                Allergies = patient.PatientAllergies.Select(pa => new AllergyViewModel
                {
                    AllergyID = pa.AllergyID,
                    AllergyName = pa.Allergy != null ? pa.Allergy.AllergyName : "Unknown",
                    Category = pa.Allergy != null ? pa.Allergy.Category?.CategoryName ?? "Unknown" : "Unknown",
                    Severity = pa.Severity
                }).ToList(),
                Medications = patient.PatientMedications
                    .Where(pm => pm.IsCurrent)
                    .Select(pm => new MedicationViewModel
                    {
                        MedicationID = pm.MedicationID,
                        MedicationName = pm.Medication != null ? pm.Medication.MedicationName : "Unknown",
                        Category = pm.Medication != null ? pm.Medication.Category?.CategoryName ?? "Unknown" : "Unknown",
                        Dosage = pm.Dosage,
                        IsCurrent = pm.IsCurrent
                    }).ToList(),
                RecentRequests = patient.TestRequests
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .Select(r => new TestRequestSummaryViewModel
                    {
                        RequestID = r.RequestID,
                        RequestNumber = r.RequestNumber,
                        RequestDate = r.RequestDate,
                        Status = r.Status,
                        Tests = r.RequestTestTypes.Select(rt => rt.TestType != null ? rt.TestType.TestName : "Unknown").ToList()
                    }).ToList()
            };
        }

        
        // RESULTS
    
        public async Task<ResultsIndexViewModel> GetResultsAsync(int doctorId, ResultsFilterViewModel filter)
        {
            var query = _context.TestRequests
                .Where(r => r.DoctorID == doctorId && (r.Status == "Verified" || r.Status == "Completed" || r.Status == "Released"))
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestType)
                .Include(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestResults)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(r => r.RequestNumber.Contains(filter.SearchTerm) ||
                    (r.Patient != null && (r.Patient.FirstName.Contains(filter.SearchTerm) || r.Patient.LastName.Contains(filter.SearchTerm))));
            }

            if (!string.IsNullOrEmpty(filter.Status) && filter.Status == "pending")
            {
                query = query.Where(r => r.RequestTestTypes.Any(rt => rt.TestResults != null && rt.TestResults.Any(t => !t.IsAbnormal)));
            }

            if (!string.IsNullOrEmpty(filter.Status) && filter.Status == "released")
            {
                // Filter for released results
            }

            if (!string.IsNullOrEmpty(filter.AbnormalFilter) && filter.AbnormalFilter == "abnormal")
            {
                query = query.Where(r => r.RequestTestTypes.Any(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal)));
            }

            var results = await query.ToListAsync();

            return new ResultsIndexViewModel
            {
                Results = results.Select(r => new ResultGroupViewModel
                {
                    RequestID = r.RequestID,
                    RequestNumber = r.RequestNumber,
                    PatientName = r.Patient != null ? $"{r.Patient.FirstName} {r.Patient.LastName}" : "Unknown",
                    RequestDate = r.RequestDate,
                    TestResults = r.RequestTestTypes
                        .Where(rt => rt.TestResults != null && rt.TestResults.Any())
                        .SelectMany(rt => rt.TestResults.Select(t => new ResultItemViewModel
                        {
                            ResultID = t.ResultID,
                            TestName = rt.TestType != null ? rt.TestType.TestName : "Unknown",
                            ResultValue = t.ResultValue ?? "N/A",
                            NormalRange = rt.TestType != null ? $"{rt.TestType.NormalRangeMin} - {rt.TestType.NormalRangeMax}" : "N/A",
                            IsAbnormal = t.IsAbnormal,
                            IsReleased = false,
                            ResultDate = t.ResultDate,
                            Notes = t.Notes
                        })).ToList()
                }).ToList(),
                Statistics = new ResultsStatisticsViewModel
                {
                    TotalResults = await _context.TestResults
                        .Where(r => r.RequestTestType != null && r.RequestTestType.TestRequest != null && r.RequestTestType.TestRequest.DoctorID == doctorId)
                        .CountAsync(),
                    AbnormalResults = await _context.TestResults
                        .Where(r => r.IsAbnormal && r.RequestTestType != null && r.RequestTestType.TestRequest != null && r.RequestTestType.TestRequest.DoctorID == doctorId)
                        .CountAsync(),
                    PendingRelease = await _context.TestRequests
                        .CountAsync(r => r.DoctorID == doctorId && r.Status == "Verified")
                }
            };
        }

        public async Task<ResultItemViewModel> GetResultDetailsAsync(int resultId)
        {
            var result = await _context.TestResults
                .Include(r => r.RequestTestType)
                .ThenInclude(rt => rt.TestType)
                .Include(r => r.RequestTestType)
                .ThenInclude(rt => rt.TestRequest)
                .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(r => r.ResultID == resultId);

            if (result == null)
                throw new ArgumentException($"Result with ID {resultId} not found");

            return new ResultItemViewModel
            {
                ResultID = result.ResultID,
                TestName = result.RequestTestType != null && result.RequestTestType.TestType != null ? result.RequestTestType.TestType.TestName : "Unknown",
                ResultValue = result.ResultValue ?? "N/A",
                NormalRange = result.RequestTestType != null && result.RequestTestType.TestType != null ?
                    $"{result.RequestTestType.TestType.NormalRangeMin} - {result.RequestTestType.TestType.NormalRangeMax}" : "N/A",
                IsAbnormal = result.IsAbnormal,
                IsReleased = false,
                ResultDate = result.ResultDate,
                Notes = result.Notes,
                Interpretation = result.IsAbnormal ? "This result is outside the normal reference range. Clinical correlation recommended." : "Within normal reference range."
            };
        }

        public async Task<bool> ReleaseResultAsync(int resultId, string note, string releaseMethod, int doctorId)
        {
            var result = await _context.TestResults
                .Include(r => r.RequestTestType)
                .ThenInclude(rt => rt.TestRequest)
                .FirstOrDefaultAsync(r => r.ResultID == resultId);

            if (result == null) return false;

            var request = result.RequestTestType?.TestRequest;
            if (request != null)
            {
                request.ReleasedToPatientDate = DateTime.Now;
                request.Status = "Released";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        
        // ALERTS
        
        public async Task<AlertsIndexViewModel> GetAlertsAsync(int doctorId, AlertsFilterViewModel filter)
        {
            var query = _context.TestRequests
                .Where(r => r.DoctorID == doctorId)
                .Include(r => r.Patient)
                .Include(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestType)
                .Include(r => r.RequestTestTypes)
                .ThenInclude(rt => rt.TestResults)
                .Where(r => r.RequestTestTypes.Any(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal)))
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(r => r.RequestNumber.Contains(filter.SearchTerm) ||
                    (r.Patient != null && (r.Patient.FirstName.Contains(filter.SearchTerm) || r.Patient.LastName.Contains(filter.SearchTerm))));
            }

            if (!string.IsNullOrEmpty(filter.Urgency) && filter.Urgency != "all")
            {
                query = query.Where(r => r.Urgency == filter.Urgency);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(r => r.RequestDate <= filter.EndDate.Value);
            }

            var alerts = await query.ToListAsync();

            return new AlertsIndexViewModel
            {
                Alerts = alerts.Select(r => new AlertGroupViewModel
                {
                    RequestID = r.RequestID,
                    RequestNumber = r.RequestNumber,
                    PatientName = r.Patient != null ? $"{r.Patient.FirstName}   {r.Patient.LastName}" : "Unknown",
                    AlertDate = r.RequestDate,
                    Urgency = r.Urgency,
                    AbnormalTests = r.RequestTestTypes
                        .Where(rt => rt.TestResults != null && rt.TestResults.Any(t => t.IsAbnormal))
                        .SelectMany(rt => rt.TestResults.Where(t => t.IsAbnormal).Select(t => new AbnormalTestViewModel
                        {
                            TestName = rt.TestType != null ? rt.TestType.TestName : "Unknown",
                            Result = t.ResultValue ?? "N/A",
                            NormalRange = rt.TestType != null ? $"{rt.TestType.NormalRangeMin} - {rt.TestType.NormalRangeMax}" : "N/A",
                            Severity = t.IsAbnormal ? "Abnormal" : "Normal",
                            ClinicalSignificance = "Clinical correlation recommended"
                        })).ToList()
                }).ToList(),
                Statistics = new AlertStatisticsViewModel
                {
                    TotalAlerts = alerts.Count,
                    CriticalCount = alerts.Count(r => r.Urgency == "stat"),
                    UrgentCount = alerts.Count(r => r.Urgency == "urgent")
                },
                Filter = filter
            };
        }

        public async Task<bool> AcknowledgeAlertAsync(int alertId, int doctorId)
        {
            _logger.LogInformation($"Alert {alertId} acknowledged by doctor {doctorId}");
            return true;
        }

        public async Task<byte[]> GenerateReportAsync(ReportFilterViewModel filter)
        {
            return await Task.FromResult(new byte[0]);
        }
    }
}