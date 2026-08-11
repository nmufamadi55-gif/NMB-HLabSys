using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NMB_HLabSys.Models.ViewModels
{
   
    // DASHBOARD VIEWMODELS
   
    public class DoctorDashboardViewModel
    {
        public int ActiveRequests { get; set; }
        public int AbnormalAlerts { get; set; }
        public int PendingReleaseCount { get; set; }
        public int TotalPatients { get; set; }
        public List<RecentRequestViewModel> RecentRequests { get; set; } = new();
        public List<AbnormalAlertViewModel> RecentAlerts { get; set; } = new();
        public DashboardStatsViewModel Stats { get; set; } = new();
    }

    public class DashboardStatsViewModel
    {
        public int RequestsThisMonth { get; set; }
        public int CompletedThisMonth { get; set; }
        public int AbnormalThisMonth { get; set; }
        public int PendingThisMonth { get; set; }
        public double CompletionRate { get; set; }
    }

    public class RecentRequestViewModel
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public int Progress { get; set; }
    }

    public class AbnormalAlertViewModel
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public string NormalRange { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public DateTime AlertDate { get; set; }
    }

    
    // TEST REQUESTS VIEWMODELS
    
    public class TestRequestIndexViewModel
    {
        public List<TestRequestListViewModel> Requests { get; set; } = new();
        public TestRequestFilterViewModel Filter { get; set; } = new();
        public TestRequestStatisticsViewModel Statistics { get; set; } = new();
    }

    public class TestRequestListViewModel
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public List<string> Tests { get; set; } = new();
        public string Barcode { get; set; } = string.Empty;
        public int Progress { get; set; }
        public bool HasAbnormal { get; set; }
        public bool HasResults { get; set; }
        public bool CanCancel { get; set; }
    }

    public class TestRequestFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Urgency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class TestRequestStatisticsViewModel
    {
        public int Total { get; set; }
        public int Submitted { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Verified { get; set; }
        public int Released { get; set; }
        public int Abnormal { get; set; }
    }

    
    // CREATE TEST REQUEST
  
    public class CreateTestRequestViewModel
    {
        [Required]
        public int PatientID { get; set; }

        [Required]
        public string Urgency { get; set; } = "routine";

        public string? ClinicalNotes { get; set; }

        [Required]
        public List<int> SelectedTestTypeIDs { get; set; } = new();

        public PatientMedicalInfoViewModel PatientInfo { get; set; } = new();
        public List<SampleRequirementViewModel> SampleRequirements { get; set; } = new();
        public List<GeneratedBarcodeViewModel> GeneratedBarcodes { get; set; } = new();
    }

    public class SampleRequirementViewModel
    {
        public string SampleType { get; set; } = string.Empty;
        public string CollectionTube { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public List<string> TestNames { get; set; } = new();
    }

    public class GeneratedBarcodeViewModel
    {
        public string Barcode { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string SampleType { get; set; } = string.Empty;
    }

    
    // PATIENTS VIEWMODELS
   
    public class PatientIndexViewModel
    {
        public List<PatientListViewModel> Patients { get; set; } = new();
        public PatientStatisticsViewModel Statistics { get; set; } = new();
        public string? SearchTerm { get; set; }
    }

    public class PatientListViewModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SAIDNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Cellphone { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
        public List<string> Allergies { get; set; } = new();
        public List<string> Medications { get; set; } = new();
    }

    public class PatientStatisticsViewModel
    {
        public int Total { get; set; }
        public int ActiveThisMonth { get; set; }
        public int WithAbnormalResults { get; set; }
    }

    public class PatientMedicalInfoViewModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SAIDNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Cellphone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<ConditionViewModel> Conditions { get; set; } = new();
        public List<AllergyViewModel> Allergies { get; set; } = new();
        public List<MedicationViewModel> Medications { get; set; } = new();
        public List<TestRequestSummaryViewModel> RecentRequests { get; set; } = new();
    }

    public class ConditionViewModel
    {
        public int ConditionID { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime? DiagnosedDate { get; set; }
    }

    public class AllergyViewModel
    {
        public int AllergyID { get; set; }
        public string AllergyName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class MedicationViewModel
    {
        public int MedicationID { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
    }

    public class TestRequestSummaryViewModel
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Tests { get; set; } = new();
    }

    
    // REGISTER PATIENT
    
    public class RegisterPatientViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        [StringLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "SA ID number is required")]
        [StringLength(13, MinimumLength = 13)]
        [RegularExpression(@"^[0-9]{13}$", ErrorMessage = "ID must be 13 digits")]
        public string SAIDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Cellphone number is required")]
        [StringLength(20)]
        public string Cellphone { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Address { get; set; }

        public List<int> SelectedConditionIDs { get; set; } = new();
        public List<int> SelectedAllergyIDs { get; set; } = new();
        public List<int> SelectedMedicationIDs { get; set; } = new();
    }

   
    // RESULTS VIEWMODELS
   
    public class ResultsIndexViewModel
    {
        public List<ResultGroupViewModel> Results { get; set; } = new();
        public ResultsFilterViewModel Filter { get; set; } = new();
        public ResultsStatisticsViewModel Statistics { get; set; } = new();
    }

    public class ResultGroupViewModel
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public List<ResultItemViewModel> TestResults { get; set; } = new();
        public bool HasPendingRelease => TestResults.Any(r => !r.IsReleased);
        public bool HasAbnormal => TestResults.Any(r => r.IsAbnormal);
    }

    public class ResultItemViewModel
    {
        public int ResultID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public string NormalRange { get; set; } = string.Empty;
        public bool IsAbnormal { get; set; }
        public bool IsReleased { get; set; }
        public DateTime ResultDate { get; set; }
        public string? Notes { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }

    public class ResultsFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? AbnormalFilter { get; set; }
    }

    public class ResultsStatisticsViewModel
    {
        public int TotalResults { get; set; }
        public int AbnormalResults { get; set; }
        public int PendingRelease { get; set; }
        public int Released { get; set; }
    }

    // ALERTS VIEWMODELS
    
    public class AlertsIndexViewModel
    {
        public List<AlertGroupViewModel> Alerts { get; set; } = new();
        public AlertStatisticsViewModel Statistics { get; set; } = new();
        public AlertsFilterViewModel Filter { get; set; } = new();
    }

    public class AlertGroupViewModel
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime AlertDate { get; set; }
        public string Urgency { get; set; } = string.Empty;
        public List<AbnormalTestViewModel> AbnormalTests { get; set; } = new();
    }

    public class AbnormalTestViewModel
    {
        public string TestName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string NormalRange { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string ClinicalSignificance { get; set; } = string.Empty;
    }

    public class AlertStatisticsViewModel
    {
        public int TotalAlerts { get; set; }
        public int CriticalCount { get; set; }
        public int UrgentCount { get; set; }
    }

    public class AlertsFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Urgency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    
    // REPORTS VIEWMODELS
    
    public class ReportsIndexViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new();
        public ReportStatisticsViewModel Statistics { get; set; } = new();
        public List<SavedReportViewModel> SavedReports { get; set; } = new();
    }

    
    public class ReportStatisticsViewModel
    {
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int AbnormalResults { get; set; }
        public int PendingRequests { get; set; }
        public double CompletionRate { get; set; }
    }

    public class SavedReportViewModel
    {
        public string ReportName { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
    }

   
    // RELEASE RESULTS
   
    public class ReleaseResultsViewModel
    {
        [Required]
        public int ResultID { get; set; }

        public string PatientName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;

        [Display(Name = "Note to Patient")]
        public string? Note { get; set; }

        [Required]
        [Display(Name = "Release Method")]
        public string ReleaseMethod { get; set; } = "appointment";

        public List<SelectListItem> ReleaseMethods { get; set; } = new();
    }


    // PATIENT CONSENT VIEWMODEL
   
    public class PatientConsentViewModel
    {
        public int ConsentID { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorEmail { get; set; } = string.Empty;
        public DateTime GrantedDate { get; set; }
        public DateTime? RevokedDate { get; set; }
        public bool IsActive { get; set; }
        public List<ConsentTestRequestViewModel> TestRequests { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class ConsentTestRequestViewModel
    {
        public int TestRequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}