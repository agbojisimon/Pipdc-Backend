using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PIPDC.Infrastructure.RateLimiting;

/// <summary>
/// Concurrency limiter for the image upload endpoint. Uploads are expensive
/// (slow I/O, memory), so it matters how many run at once, not how many per minute.
/// Capping concurrent in-flight uploads protects the server from being swamped by
/// parallel requests while not artificially throttling a steady stream.
/// </summary>
public sealed class UploadsRateLimitPolicy : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        var key = RateLimitPartitioners.UserOrIp(context);

        return RateLimitPartition.GetConcurrencyLimiter(key, _ => new ConcurrencyLimiterOptions
        {
            PermitLimit = 4,
            QueueLimit = 0
        });
    }
}
