using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PIPDC.Infrastructure.Data;

namespace PIPDC.Infrastructure.HealthChecks;

/// <summary>
/// Registers the health-check checks only. This is the only place that configures
/// <c>AddHealthChecks</c>; Program.cs never does. The database check is tagged
/// "ready" so it runs only for the readiness probe, and a 2s timeout keeps a slow
/// or hung database from stalling that probe.
/// </summary>
public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddDatabaseHealthCheck(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(
                name: "database",
                tags: new[] { "ready" });

        // This EF Core extension does not expose a timeout parameter, so apply the
        // 2s cap on the generated registration.
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            var registration = options.Registrations.FirstOrDefault(r => r.Name == "database");
            if (registration is not null)
            {
                registration.Timeout = TimeSpan.FromSeconds(2);
            }
        });

        return services;
    }
}