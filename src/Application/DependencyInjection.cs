using Microsoft.Extensions.DependencyInjection;
using PIPDC.Application.Agents;
using PIPDC.Application.Properties;

namespace PIPDC.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IAgentService, AgentService>();

        return services;
    }
}
