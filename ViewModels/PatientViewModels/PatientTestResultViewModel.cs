using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientTestResultViewModel
    {
        public int TestRequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string Urgency { get; set; }
        public List<TestResultDetailViewModel> TestResults { get; set; }
    }

    public class TestResultDetailViewModel
    {
        public int TestResultID { get; set; }
        public string TestName { get; set; }
        public string ResultValue { get; set; }
        public string NormalRange { get; set; }
        public string Unit { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime CompletedDate { get; set; }
        public string Notes { get; set; }
    }
}