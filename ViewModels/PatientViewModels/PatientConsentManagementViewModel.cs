using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientConsentManagementViewModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; }
        public List<PatientConsentViewModel> ActiveConsents { get; set; } = new List<PatientConsentViewModel>();
        public List<DoctorViewModel> AvailableDoctors { get; set; } = new List<DoctorViewModel>();
        public List<TestRequestViewModel> AvailableTestRequests { get; set; } = new List<TestRequestViewModel>();
    }

    public class PatientConsentViewModel
    {
        public int ConsentID { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorEmail { get; set; } = string.Empty;
        public DateTime GrantedDate { get; set; } = DateTime.Now;
        public DateTime? RevokedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
    }

    public class DoctorViewModel
    {
        public int DoctorID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HPCSANumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
    }

    public class TestRequestViewModel
    {
        public int TestRequestID { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = string.Empty;
    }
}