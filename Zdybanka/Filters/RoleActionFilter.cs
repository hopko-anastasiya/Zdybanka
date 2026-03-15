using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zdybanka.Controllers;

namespace Zdybanka.Filters
{
    public class RoleActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                controller.ViewData["Role"] = HomeController.CurrentRole;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
