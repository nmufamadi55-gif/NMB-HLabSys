using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.TotalDoctors = await _context.Doctors.CountAsync();
            ViewBag.TotalTestTypes = await _context.TestTypes.CountAsync();
            ViewBag.TotalTestsPerformed = await _context.TestResults.CountAsync();

            return View();
        }
    }
}