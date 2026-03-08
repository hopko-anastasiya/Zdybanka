using Microsoft.AspNetCore.Mvc;

namespace Zdybanka.Controllers
{
    public class Organization_interfaceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
