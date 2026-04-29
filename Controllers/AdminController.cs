using Microsoft.AspNetCore.Mvc;

namespace NMB_HLabSys.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Conditions()
        {
            return View();
        }

        public IActionResult Allergies()
        {
            return View();
        }

        public IActionResult Medications()
        {
            return View();
        }
    }
}