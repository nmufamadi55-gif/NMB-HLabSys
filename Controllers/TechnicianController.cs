using Microsoft.AspNetCore.Mvc;

namespace NMB_HLabSys.Controllers
{
    public class TechnicianController : Controller
    {
        public IActionResult Dashboard()
        {
            ViewBag.TechName = "Michael Peters";
            ViewBag.EmployeeNumber = "T2024001";
            return View();
        }

        public IActionResult ReceiveSamples() => View();
        public IActionResult PerformTests() => View();
        public IActionResult Verification() => View();
    }
}