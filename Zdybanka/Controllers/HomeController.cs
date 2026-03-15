using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Zdybanka.Models;

namespace Zdybanka.Controllers
{
    public class HomeController : Controller
    {
        public const string CurrentRole = "Organization";

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return CurrentRole switch
            {
                "User" => RedirectToAction("Index", "Events"),
                "Organization" => RedirectToAction("Profile", "Organizations", new { id = TemporaryIdentity.CurrentOrganizationId }),
                "Admin" => RedirectToAction("Index", "Organizations"),
                _ => RedirectToAction("Index", "Events")
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
