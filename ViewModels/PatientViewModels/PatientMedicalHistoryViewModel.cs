using System;
using System.Collections.Generic;
using NMB_HLabSys.Models;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientMedicalHistoryViewModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; }

        // Patient's current medical history
        public List<PatientConditionViewModel> Conditions { get; set; } = new List<PatientConditionViewModel>();
        public List<PatientAllergyViewModel> Allergies { get; set; } = new List<PatientAllergyViewModel>();
        public List<PatientMedicationViewModel> Medications { get; set; } = new List<PatientMedicationViewModel>();

        // Available options for adding new items
        public List<MedicalCondition> AvailableConditions { get; set; } = new List<MedicalCondition>();
        public List<Allergy> AvailableAllergies { get; set; } = new List<Allergy>();
        public List<Medication> AvailableMedications { get; set; } = new List<Medication>();
    }
}