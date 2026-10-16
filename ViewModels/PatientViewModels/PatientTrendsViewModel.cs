using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientTrendsViewModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public List<string> AvailableTestTypes { get; set; } = new();
        public string SelectedTestType { get; set; } = string.Empty;

        // ✅ NEW: Date range instead of months
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<TrendDataPoint> TrendData { get; set; } = new();
    }

    public class TrendDataPoint
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public double? LowerLimit { get; set; }
        public double? UpperLimit { get; set; }
    }
}