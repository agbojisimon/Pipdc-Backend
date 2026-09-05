using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PIPDC.Infrastructure.Data;

namespace PIPDC.Infrastructure.HealthChecks;

/// <summary>
/// Maps the liveness and readiness probe endpoints.
///
/// <see href="/healthz/live"/>: predicate excludes every check, so it only verifies
/// the process is up and responding. A liveness failure means the process is dead
/// or stuck and should be restarted.
///
/// <see href="/healthz/ready"/>: runs only checks tagged "ready" (the database).
/// Returns 503 when a dependency is down so the orchestrator pulls this instance
/// out of rotation without killing it, avoiding crash-loops on a dependency blip.
/// </summary>
public static class HealthCheckEndpointExtensions
{
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        endpoints.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        durationMs = e.Value.Duration.TotalMilliseconds
                    })
                });
            }
        });

        return endpoints;
    }
}