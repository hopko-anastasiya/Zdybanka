using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zdybanka.Models;
using Zdybanka.Services;

namespace Zdybanka.Filters
{
    public class RoleActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                var dbContext = context.HttpContext.RequestServices.GetService(typeof(Lab1Context)) as Lab1Context;
                if (dbContext == null)
                {
                    controller.ViewData["Role"] = "Guest";
                    controller.ViewData["IsAuthenticated"] = false;
                    return;
                }

                var mockAuth = context.HttpContext.RequestServices.GetService(typeof(IAppAuthenticationService)) as IAppAuthenticationService;
                var currentUserId = mockAuth?.CurrentUserId;
                var currentUser = currentUserId.HasValue
                    ? dbContext.Users.FirstOrDefault(u => u.Id == currentUserId.Value)
                    : null;

                if (currentUser == null)
                {
                    controller.ViewData["Role"] = "Guest";
                    controller.ViewData["IsAuthenticated"] = false;
                    return;
                }

                controller.ViewData["IsAuthenticated"] = true;
                controller.ViewData["CurrentUserId"] = currentUser.Id;
                controller.ViewData["CurrentUserName"] = currentUser.Fullname;

                controller.ViewData["Role"] = currentUser.Role switch
                {
                    UserRole.Admin => "Admin",
                    UserRole.OrganizationManager => "Organization",
                    _ => "User"
                };

                var currentOrganizationId = dbContext.Organizations
                    .Where(o => o.Id == currentUser.Id)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefault();

                if (currentOrganizationId.HasValue)
                {
                    controller.ViewData["CurrentOrganizationId"] = currentOrganizationId.Value;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

