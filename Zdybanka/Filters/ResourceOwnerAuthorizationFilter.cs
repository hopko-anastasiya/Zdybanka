using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Zdybanka.Models;
using Zdybanka.Services;
using System.Security.Claims;

namespace Zdybanka.Filters;

public class ResourceOwnerAuthorizationFilter : IAsyncActionFilter
{
    private readonly Lab1Context _context;
    private readonly IAppAuthenticationService _auth;

    public ResourceOwnerAuthorizationFilter(Lab1Context context, IAppAuthenticationService auth)
    {
        _context = context;
        _auth = auth;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();
        var idValue = context.RouteData.Values["id"]?.ToString();
        
        if (string.IsNullOrEmpty(idValue) || !int.TryParse(idValue, out int id))
        {
            await next();
            return;
        }

        var currentUserId = _auth.CurrentUserId;
        if (!currentUserId.HasValue) 
        {
            await next();
            return;
        }

        if (controllerName == "Organizations")
        {
            // For Organizations, ID in route is the Org ID
            if (id != currentUserId.Value && !context.HttpContext.User.IsInRole(nameof(UserRole.Admin)))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
        else if (controllerName == "Events")
        {
            // Exclude Details action so any user can see an event
            if (actionName == "Edit" || actionName == "Delete" || actionName == "DeleteConfirmed")
            {
                var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
                if (ev != null && ev.Organizationid != currentUserId.Value && !context.HttpContext.User.IsInRole(nameof(UserRole.Admin)))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
        else if (controllerName == "Userfavorites")
        {
            var fav = await _context.Userfavorites.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (fav != null && fav.Userid != currentUserId.Value && !context.HttpContext.User.IsInRole(nameof(UserRole.Admin)))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
        else if (controllerName == "Registrations")
        {
            var reg = await _context.Registrations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (reg != null && reg.Userid != currentUserId.Value && !context.HttpContext.User.IsInRole(nameof(UserRole.Admin)))
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        await next();
    }
}
