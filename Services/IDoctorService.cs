using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;

namespace NMB_HLabSys.Services
{
    public interface IDoctorService
    {
        // Dashboard
        Task<DoctorDashboardViewModel> GetDashboardDataAsync(int doctorId);

        // Test Requests
        Task<TestRequestIndexViewModel> GetTestRequestsAsync(int doctorId, TestRequestFilterViewModel filter);
        Task<TestRequest> CreateTestRequestAsync(CreateTestRequestViewModel model, int doctorId);
        Task<TestRequest> GetTestRequestDetailsAsync(int requestId);
        Task<bool> CancelTestRequestAsync(int requestId, string reason);
        Task<bool> UpdateTestRequestStatusAsync(int requestId, string status);

        // Patients
        Task<PatientIndexViewModel> GetPatientsAsync(int doctorId, string? searchTerm = null);
        Task<Patient> RegisterPatientAsync(RegisterPatientViewModel model, int doctorId);
        Task<PatientMedicalInfoViewModel> GetPatientMedicalInfoAsync(int patientId);

        // Results
        Task<ResultsIndexViewModel> GetResultsAsync(int doctorId, ResultsFilterViewModel filter);
        Task<ResultItemViewModel> GetResultDetailsAsync(int resultId);
        Task<bool> ReleaseResultAsync(int resultId, string note, string releaseMethod, int doctorId);

        // Alerts
        Task<AlertsIndexViewModel> GetAlertsAsync(int doctorId, AlertsFilterViewModel filter);
        Task<bool> AcknowledgeAlertAsync(int alertId, int doctorId);

        // Reports
        Task<byte[]> GenerateReportAsync(ReportFilterViewModel filter);
    }
}