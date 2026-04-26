using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zdybanka.Models;

namespace Zdybanka.Services;

public class AppAuthenticationService : IAppAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Lab1Context _context;

    public AppAuthenticationService(IHttpContextAccessor httpContextAccessor, Lab1Context context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public int? CurrentUserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true && int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id))
            {
                return id;
            }
            return null;
        }
    }

    public bool IsAuthenticated => CurrentUserId.HasValue;

    public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!CurrentUserId.HasValue) return null;
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == CurrentUserId.Value, cancellationToken);
    }
}
