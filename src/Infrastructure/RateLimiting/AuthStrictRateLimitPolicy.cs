using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PIPDC.Infrastructure.RateLimiting;

/// <summary>
/// Sliding-window limiter for authentication endpoints (login, register, forgot/reset
/// password, email verification). These are brute-force / enumeration targets, so a
/// deliberately tight per-IP allowance is used. Sliding window (not fixed) is used so
/// the effective rate is a true "5 per rolling minute": a client cannot double its
/// budget by straddling a fixed-window boundary (e.g. 5 requests at 00:59 and 5 more
/// at 01:00). That boundary-burst hole is exactly what a login hammer would exploit.
/// </summary>
public sealed class AuthStrictRateLimitPolicy : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        var key = $"ip:{context.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(3),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }
}