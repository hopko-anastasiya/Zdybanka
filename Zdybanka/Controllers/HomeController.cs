using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zdybanka.Models;
using Zdybanka.Services;

namespace Zdybanka.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Lab1Context _context;
        private readonly IAppAuthenticationService _AppAuthenticationService;

        public HomeController(ILogger<HomeController> logger, Lab1Context context, IAppAuthenticationService AppAuthenticationService)
        {
            _logger = logger;
            _context = context;
            _AppAuthenticationService = AppAuthenticationService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _AppAuthenticationService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return RedirectToAction("Index", "Events");
            }

            return currentUser.Role switch
            {
                UserRole.Admin => RedirectToAction("Organizations", "Admin_interface"),
                UserRole.OrganizationManager => await RedirectOrganizationManagerAsync(),
                UserRole.User => RedirectToAction("Index", "Events"),
                _ => RedirectToAction("Index", "Events")
            };
        }

        private async Task<IActionResult> RedirectOrganizationManagerAsync()
        {
            var currentUserId = _AppAuthenticationService.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Index", "Events");
            }

            var organization = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == currentUserId.Value);

            if (organization == null)
            {
                return RedirectToAction("Index", "Organizations");
            }

            return RedirectToAction("Profile", "Organizations", new { id = organization.Id });
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
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

