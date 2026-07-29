using Microsoft.AspNetCore.Mvc;

namespace NMB_HLabSys.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: Admin/Conditions
        public IActionResult Conditions()
        {
            return View();
        }

        // GET: Admin/Allergies
        public IActionResult Allergies()
        {
            return View();
        }

        // GET: Admin/Medications
        public IActionResult Medications()
        {
            return View();
        }

        // GET: Admin/AddCondition
        public IActionResult AddCondition()
        {
            return View();
        }

        // POST: Admin/AddCondition
        [HttpPost]
        public IActionResult AddCondition(string conditionName, int categoryId, string description)
        {
            TempData["SuccessMessage"] = "Condition added successfully!";
            return RedirectToAction("Conditions");
        }

        // GET: Admin/AddAllergy
        public IActionResult AddAllergy()
        {
            return View();
        }

        // POST: Admin/AddAllergy
        [HttpPost]
        public IActionResult AddAllergy(string allergyName, int categoryId, string description)
        {
            TempData["SuccessMessage"] = "Allergy added successfully!";
            return RedirectToAction("Allergies");
        }

        // GET: Admin/AddMedication
        public IActionResult AddMedication()
        {
            return View();
        }

        // POST: Admin/AddMedication
        [HttpPost]
        public IActionResult AddMedication(string medicationName, int categoryId, string description)
        {
            TempData["SuccessMessage"] = "Medication added successfully!";
            return RedirectToAction("Medications");
        }

        // POST: Admin/DeleteCondition
        [HttpPost]
        public IActionResult DeleteCondition(int id)
        {
            return Json(new { success = true, message = "Condition deleted successfully" });
        }

        // POST: Admin/DeleteAllergy
        [HttpPost]
        public IActionResult DeleteAllergy(int id)
        {
            return Json(new { success = true, message = "Allergy deleted successfully" });
        }

        // POST: Admin/DeleteMedication
        [HttpPost]
        public IActionResult DeleteMedication(int id)
        {
            return Json(new { success = true, message = "Medication deleted successfully" });
        }
    }
}