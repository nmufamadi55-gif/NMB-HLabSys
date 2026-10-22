using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientRequestsViewModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public List<TestRequestSummaryViewModel> Requests { get; set; } = new();
    }

    public class TestRequestSummaryViewModel
    {
        public int TestRequestID { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int TestCount { get; set; }
        public bool CanViewResults { get; set; }   // true only if status == Released / Appointment Requested
        public bool IsCancelled { get; set; }
        public string CancellationReason { get; set; }
        public List<TestResultRowViewModel> Results { get; set; } = new();
    }

    public class TestResultRowViewModel
    {
        public int TestResultID { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string NormalRangeDisplay { get; set; } = string.Empty;
        public bool IsAbnormal { get; set; }
        public string StatusLabel { get; set; } = string.Empty; // "Normal" / "High" / "Low"
    }
}