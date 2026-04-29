using Microsoft.AspNetCore.Mvc;

namespace NMB_HLabSys.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult Dashboard()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            ViewBag.PatientSurname = "Mbeki";
            return View();
        }

        public IActionResult MedicalHistory()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            ViewBag.PatientSurname = "Mbeki";
            return View();
        }

        public IActionResult Consent()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            ViewBag.PatientSurname = "Mbeki";
            return View();
        }

        public IActionResult Trends()
        {
            ViewBag.PatientName = "Thabo Mbeki";
            ViewBag.PatientID = "8001015009087";
            return View();
        }
    }
}