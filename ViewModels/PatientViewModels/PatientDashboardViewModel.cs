using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientDashboardViewModel
    {
        public int PatientID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string SAIDNumber { get; set; }
        public string Email { get; set; }
        public string Cellphone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int TotalTestRequests { get; set; }
        public int AbnormalResultsCount { get; set; }
        public int ReportsAvailable { get; set; }
        public int HealthScore { get; set; }
        public string HealthStatusMessage { get; set; }
        public string CholesterolTrend { get; set; }
        public List<RecentTestResultViewModel> RecentTestResults { get; set; }
        public List<UpcomingTestViewModel> UpcomingTests { get; set; }
    }

    public class RecentTestResultViewModel
    {
        public int TestRequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string TestName { get; set; }
        public string ResultValue { get; set; }
        public string NormalRange { get; set; }
        public string Status { get; set; }
        public string DoctorName { get; set; }
        public string ReportUrl { get; set; }
        public bool IsAbnormal { get; set; }
    }

    public class UpcomingTestViewModel
    {
        public string TestName { get; set; }
        public DateTime RequestDate { get; set; }
    }
}
