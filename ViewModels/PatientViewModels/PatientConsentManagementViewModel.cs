using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientConsentManagementViewModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; }
        public List<PatientConsentManagementViewModel> ActiveConsents { get; set; }
        public List<DoctorViewModel> AvailableDoctors { get; set; }
        public List<TestRequestViewModel> AvailableTestRequests { get; set; }
    }

    public class PatientConsentViewModel
    {
        public int ConsentID { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; }
        public string DoctorEmail { get; set; }
        public DateTime GrantedDate { get; set; }
        public DateTime? RevokedDate { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; }
    }

    public class DoctorViewModel
    {
        public int DoctorID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string HPCSANumber { get; set; }
    }

    public class TestRequestViewModel
    {
        public int TestRequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
    }
}