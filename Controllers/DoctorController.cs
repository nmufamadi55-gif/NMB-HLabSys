using Microsoft.AspNetCore.Mvc;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;

namespace NMB_HLabSys.Controllers
{
    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctorService;

        public DoctorController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Dashboard()
        {
            
            var doctorId = 1;
            var viewModel = await _doctorService.GetDashboardDataAsync(doctorId);

            ViewBag.DoctorName = "Dr. Sarah Johnson";
            ViewBag.HPCSANumber = "MP1234567";
            ViewBag.UserRole = "Doctor";
            return View(viewModel);
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> TestRequests()
        {
            var doctorId = 1;
            var filter = new TestRequestFilterViewModel();
            var viewModel = await _doctorService.GetTestRequestsAsync(doctorId, filter);

            ViewBag.DoctorName = "Dr. Sarah Johnson";
            ViewBag.UserRole = "Doctor";
            return View(viewModel);
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Patients()
        {
            var doctorId = 1;
            var viewModel = await _doctorService.GetPatientsAsync(doctorId);

            ViewBag.DoctorName = "Dr. Sarah Johnson";
            ViewBag.UserRole = "Doctor";
            return View(viewModel);
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Alerts()
        {
            var doctorId = 1;
            var filter = new AlertsFilterViewModel();
            var viewModel = await _doctorService.GetAlertsAsync(doctorId, filter);

            ViewBag.DoctorName = "Dr. Sarah Johnson";
            ViewBag.UserRole = "Doctor";
            return View(viewModel);
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Results()
        {
            var doctorId = 1;
            var filter = new ResultsFilterViewModel();
            var viewModel = await _doctorService.GetResultsAsync(doctorId, filter);

            ViewBag.DoctorName = "Dr. Sarah Johnson";
            ViewBag.UserRole = "Doctor";
            return View(viewModel);
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Reports()
        {
            ViewBag.DoctorName = "Dr. Sarah Johnson";
            ViewBag.UserRole = "Doctor";
            return View();
        }

        public IActionResult Profile()
        {
            ViewBag.DoctorName = "Dr. Sarah Johnson";
            ViewBag.UserRole = "Doctor";
            return View();
        }
    }
}