using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PIPDC.Infrastructure.RateLimiting;

/// <summary>
/// Registers all rate-limiting policies and the global limiter. This is the only
/// place that configures RateLimiterOptions; Program.cs never does. Each policy is a
/// dedicated class so algorithms/limits can be tuned (and unit-tested) independently.
/// </summary>
public static class RateLimitServiceExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Shared 429 response used by every policy. The policy classes return
            // RateLimitLease.None so the framework falls back to this global handler.
            // When the limiter reports a Retry-After, surface it so clients know when
            // they may retry, and return a standards-compliant problem+json body.
            options.OnRejected = async (context, ct) =>
            {
                var response = context.HttpContext.Response;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter =
                        retryAfter.TotalSeconds.ToString("0", CultureInfo.InvariantCulture);
                }

                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.ContentType = "application/problem+json";

                await response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too many requests",
                    status = 429,
                    detail = "Slow down and try again shortly."
                }, ct);
            };

            var globalPolicy = new GlobalRateLimitPolicy();
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => globalPolicy.GetPartition(context));

            options.AddPolicy<string>(RateLimitPolicies.AuthStrict, new AuthStrictRateLimitPolicy());
            options.AddPolicy<string>(RateLimitPolicies.Writes, new WritesRateLimitPolicy());
            options.AddPolicy<string>(RateLimitPolicies.Uploads, new UploadsRateLimitPolicy());
        });

        return services;
    }
}
