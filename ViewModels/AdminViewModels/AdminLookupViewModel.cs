using System.Collections.Generic;
using NMB_HLabSys.Models;

namespace NMB_HLabSys.ViewModels.AdminViewModels
{
    public class AdminLookupViewModel
    {
        public List<LookupCategory> Categories { get; set; } = new List<LookupCategory>();
        public List<MedicalCondition> Conditions { get; set; } = new List<MedicalCondition>();
        public List<Allergy> Allergies { get; set; } = new List<Allergy>();
        public List<Medication> Medications { get; set; } = new List<Medication>();
        public string SelectedCategoryType { get; set; } = string.Empty;
        public int? SelectedCategoryID { get; set; }
    }
}