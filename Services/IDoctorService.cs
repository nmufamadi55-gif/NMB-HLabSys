// File: Services/IDoctorService.cs
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;

namespace NMB_HLabSys.Services
{
    public interface IDoctorService
    {
        // ═══════════ DASHBOARD ═══════════
        Task<DoctorDashboardViewModel> GetDashboardDataAsync(int doctorId);

        // ═══════════ TEST REQUESTS ═══════════
        Task<TestRequestIndexViewModel> GetTestRequestsAsync(int doctorId, TestRequestFilterViewModel filter);
        Task<CreateTestRequestViewModel> GetCreateTestRequestViewModelAsync(int doctorId, int? preselectedPatientId = null);
        Task<TestRequest> CreateTestRequestAsync(CreateTestRequestViewModel model, int doctorId);
        Task<TestRequest?> GetTestRequestDetailsAsync(int requestId);
        Task<bool> CancelTestRequestAsync(int requestId, string reason);
        Task<bool> UpdateTestRequestStatusAsync(int requestId, string status);

        // ═══════════ PATIENTS ═══════════
        Task<PatientIndexViewModel> GetPatientsAsync(int doctorId, string? searchTerm = null);
        Task<Patient> RegisterPatientAsync(RegisterPatientViewModel model, int doctorId);
        Task<PatientMedicalInfoViewModel> GetPatientMedicalInfoAsync(int patientId);
        Task<PatientMedicalInfoViewModel?> FindPatientBySAIDAsync(string saIdNumber);
        Task<RegisterPatientViewModel> GetRegisterPatientViewModelAsync();

        // ═══════════ RESULTS ═══════════
        Task<ResultsIndexViewModel> GetResultsAsync(int doctorId, ResultsFilterViewModel filter);
        Task<ResultItemViewModel> GetResultDetailsAsync(int resultId);
        Task<bool> ReleaseResultAsync(int resultId, string note, string releaseMethod, int doctorId);

        // ═══════════ ALERTS ═══════════
        Task<AlertsIndexViewModel> GetAlertsAsync(int doctorId, AlertsFilterViewModel filter);
        Task<bool> AcknowledgeAlertAsync(int alertId, int doctorId);

        // ═══════════ REPORTS ═══════════
        Task<byte[]> GenerateReportAsync(int doctorId, DoctorReportFilterViewModel filter);
    }
}