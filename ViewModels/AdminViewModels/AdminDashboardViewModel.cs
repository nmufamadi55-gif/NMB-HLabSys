using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.AdminViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalConditions { get; set; }
        public int TotalAllergies { get; set; }
        public int TotalMedications { get; set; }
        public int TotalCategories { get; set; }
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalTechnicians { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new List<RecentActivityViewModel>();
    }

    public class RecentActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public string UserName { get; set; } = string.Empty;  // ← Make sure this exists!
    }
}