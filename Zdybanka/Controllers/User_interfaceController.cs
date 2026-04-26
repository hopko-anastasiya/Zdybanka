using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zdybanka.Models;

namespace Zdybanka.Controllers
{
    [Authorize(Roles = nameof(UserRole.User))]
    public class User_interfaceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
