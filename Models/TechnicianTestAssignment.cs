using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models
{
    public class TechnicianTestAssignment
    {
        [Key]
        public int AssignmentID { get; set; }

        [Required]
        public int TechnicianID { get; set; }

        [Required]
        public int TestID { get; set; }

        public DateTime AssignedDate { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        [ForeignKey("TechnicianId")]
        public virtual LabTechnician Technician { get; set; }

        [ForeignKey("TestId")]
        public virtual TestType TestType
        {
            get; set;
        }
    }
}
