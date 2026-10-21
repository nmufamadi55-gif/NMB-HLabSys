using System.Collections.Generic;
using NMB_HLabSys.Models;

namespace NMB_HLabSys.ViewModels.AdminViewModels
{
    public class ManageCategoryViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}