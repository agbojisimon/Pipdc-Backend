using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PIPDC.Infrastructure.RateLimiting;

/// <summary>
/// Global limiter applied to every request (no [EnableRateLimiting] attribute needed).
/// Uses a sliding window so a client cannot double the budget by straddling a window
/// boundary. Authenticated users get a generous per-user allowance; anonymous callers
/// get a smaller per-IP allowance.
/// </summary>
public sealed class GlobalRateLimitPolicy : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        var key = RateLimitPartitioners.UserOrIp(context);
        var isAuthed = context.User.Identity?.IsAuthenticated == true;

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = isAuthed ? 300 : 60,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }
}
