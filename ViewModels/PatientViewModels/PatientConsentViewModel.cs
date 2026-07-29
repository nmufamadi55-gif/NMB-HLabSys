using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientConsentViewModel
    {
        public int ConsentID { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; }
        public string DoctorEmail { get; set; }
        public DateTime GrantedDate { get; set; }
        public DateTime? RevokedDate { get; set; }
        public bool IsActive { get; set; }
        public List<ConsentTestRequestViewModel> TestRequests { get; set; }
        public string Notes { get; set; }
    }

    public class ConsentTestRequestViewModel
    {
        public int TestRequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
    }
}