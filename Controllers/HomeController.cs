using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NMB_HLabSys.Models;


namespace NMB_HLabSys.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Privacy() => View();
        public IActionResult Terms() => View();
        public IActionResult CookiePolicy() => View();
        public IActionResult Legal() => View();
    }
}