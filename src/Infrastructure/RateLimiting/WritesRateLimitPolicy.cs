using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PIPDC.Infrastructure.RateLimiting;

/// <summary>
/// Sliding-window limiter for general "write" endpoints. Used by controllers that
/// let normal users create/update/delete data, so the budget is per user (or per IP
/// for anonymous callers) and generous enough for legitimate use. Sliding window
/// avoids the fixed-window boundary-burst hole.
/// </summary>
public sealed class WritesRateLimitPolicy : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        var key = RateLimitPartitioners.UserOrIp(context);

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }
}
