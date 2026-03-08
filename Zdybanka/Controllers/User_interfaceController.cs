using Microsoft.AspNetCore.Mvc;

namespace Zdybanka.Controllers
{
    public class User_interfaceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
