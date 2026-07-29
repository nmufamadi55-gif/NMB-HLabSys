using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NMB_HLabSys.Models;

namespace NMB_HLabSys.ViewModels.AdminViewModels
{
    public class ManageConditionViewModel
    {
        public int? ConditionID { get; set; }

        [Required]
        [StringLength(200)]
        public string ConditionName { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public List<LookupCategory> Categories { get; set; } = new List<LookupCategory>();
    }
}