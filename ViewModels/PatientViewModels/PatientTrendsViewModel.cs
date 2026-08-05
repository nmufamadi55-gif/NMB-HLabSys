using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientTrendsViewModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; }
        public List<string> AvailableTestTypes { get; set; }
        public string SelectedTestType { get; set; }
        public int Months { get; set; }
        public List<TrendDataPoint> TrendData { get; set; }
    }

    public class TrendDataPoint
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public double? LowerLimit { get; set; }
        public double? UpperLimit { get; set; }
    }
}
