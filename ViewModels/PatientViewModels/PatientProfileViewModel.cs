using System;
using System.Collections.Generic;

namespace NMB_HLabSys.ViewModels.PatientViewModels
{
    public class PatientProfileViewModel
    {
        public int PatientID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string SAIDNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Cellphone { get; set; }
        public string HomeAddress { get; set; }
        public string Email { get; set; }

        public List<PatientConditionViewModel> Conditions { get; set; } = new List<PatientConditionViewModel>();
        public List<PatientAllergyViewModel> Allergies { get; set; } = new List<PatientAllergyViewModel>();
        public List<PatientMedicationViewModel> Medications { get; set; } = new List<PatientMedicationViewModel>();
    }

    public class PatientConditionViewModel
    {
        public int PatientConditionID { get; set; }
        public int ConditionID { get; set; }
        public string ConditionName { get; set; }
        public string CategoryName { get; set; }
        public DateTime? DiagnosedDate { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
    }

    public class PatientAllergyViewModel
    {
        public int PatientAllergyID { get; set; }
        public int AllergyID { get; set; }
        public string AllergyName { get; set; }
        public string CategoryName { get; set; }
        public string Severity { get; set; }
        public string Notes { get; set; }
    }

    public class PatientMedicationViewModel
    {
        public int PatientMedicationID { get; set; }
        public int MedicationID { get; set; }
        public string MedicationName { get; set; }
        public string CategoryName { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string Notes { get; set; }
    }
}
