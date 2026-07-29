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
        public List<RecentActivityViewModel> RecentActivities { get; set; }
    }

    public class RecentActivityViewModel
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Item { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
    }
}