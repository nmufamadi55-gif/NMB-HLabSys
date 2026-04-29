using Microsoft.AspNetCore.Mvc;

namespace NMB_HLabSys.Controllers
{
    public class DoctorController : Controller
    {
        public IActionResult Dashboard()
        {
            ViewBag.DoctorName = "Dr. Sarah Johnson";
            ViewBag.HPCSANumber = "MP1234567";
            return View();
        }

        public IActionResult TestRequests()
        {
            ViewBag.DoctorName = "Dr. Sarah Johnson";
            return View();
        }

        public IActionResult Patients()
        {
            ViewBag.DoctorName = "Dr. Sarah Johnson";
            return View();
        }

        public IActionResult Alerts()
        {
            ViewBag.DoctorName = "Dr. Sarah Johnson";
            return View();
        }

        public IActionResult Results()
        {
            ViewBag.DoctorName = "Dr. Sarah Johnson";
            return View();
        }

        public IActionResult Reports()
        {
            ViewBag.DoctorName = "Dr. Sarah Johnson";
            return View();
        }
    }
}