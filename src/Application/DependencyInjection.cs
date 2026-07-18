using Microsoft.Extensions.DependencyInjection;
using PIPDC.Application.Properties;

namespace PIPDC.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPropertyService, PropertyService>();

        return services;
    }
}
