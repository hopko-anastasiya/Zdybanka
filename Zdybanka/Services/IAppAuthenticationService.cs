using Zdybanka.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Zdybanka.Services;

public interface IAppAuthenticationService
{
    int? CurrentUserId { get; }
    bool IsAuthenticated { get; }
    Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
