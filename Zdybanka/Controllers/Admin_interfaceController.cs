using Microsoft.AspNetCore.Mvc;

namespace Zdybanka.Controllers
{
    public class Admin_interfaceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
