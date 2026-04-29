using Microsoft.AspNetCore.Mvc;

namespace NMB_HLabSys.Controllers
{
    public class ManagerController : Controller
    {
        public IActionResult Dashboard()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult TestCatalogue()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult StockManagement()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult StaffManagement()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }
    }
}