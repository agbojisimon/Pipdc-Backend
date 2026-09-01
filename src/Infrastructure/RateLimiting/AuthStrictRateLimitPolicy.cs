using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PIPDC.Infrastructure.RateLimiting;

/// <summary>
/// Fixed-window limiter for authentication endpoints (login, register, forgot/reset
/// password, email verification). These are brute-force / enumeration targets, so a
/// deliberately tight per-IP allowance is used. Fixed window (not sliding) is
/// appropriate here: the boundary-burst weakness is irrelevant against a login hammer,
/// and fixed window is the cheapest possible accounting.
/// </summary>
public sealed class AuthStrictRateLimitPolicy : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        var key = $"ip:{context.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }
}
