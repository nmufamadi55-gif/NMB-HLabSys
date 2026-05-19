using Microsoft.AspNetCore.Mvc;

namespace LabManager_UI.Controllers
{
    public class ManagerController : Controller
    {
        // =========================
        // DASHBOARD
        // =========================
        public IActionResult Dashboard()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        // =========================
        // TEST CATALOGUE
        // =========================
        public IActionResult TestCatalogue()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        // =========================
        // STOCK MANAGEMENT
        // =========================
        public IActionResult StockManagement()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        // =========================
        // STAFF MANAGEMENT
        // =========================
        public IActionResult StaffManagement()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        // =========================
        // LOGIN
        // =========================
        public IActionResult Login()
        {
            return View();
        }

        // =========================
        // TECHNICIAN VIEWS
        // =========================
        public IActionResult AddTechnician()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult EditTechnician()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult TechnicianProfile()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult TechnicianPerformanceDetails()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        // =========================
        // DOCTOR VIEWS
        // =========================
        public IActionResult RegisterDoctor()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        // =========================
        // STOCK / SUPPLIER VIEWS
        // =========================
        public IActionResult ViewSupplier()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult ViewOrder()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        // =========================
        // TEST VERIFICATION
        // =========================
        public IActionResult TestVerification()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult Reports()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }

        public IActionResult Settings()
        {
            ViewBag.ManagerName = "Amanda Adams";
            return View();
        }
    }
}