using System.Security.Claims;

namespace PIPDC.Infrastructure.RateLimiting;

/// <summary>
/// Shared partition-key logic used by the limiter policies.
/// Authenticated callers are keyed by user id so they get their own bucket;
/// anonymous callers are keyed by IP address.
/// </summary>
public static class RateLimitPartitioners
{
    public static string UserOrIp(HttpContext context)
    {
        var isAuthed = context.User.Identity?.IsAuthenticated == true;
        return isAuthed
            ? $"u:{context.User.FindFirstValue(ClaimTypes.NameIdentifier)}"
            : $"ip:{context.Connection.RemoteIpAddress}";
    }
}
