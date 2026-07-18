using Microsoft.AspNetCore.Identity;
using PIPDC.Application.Auth;

namespace PIPDC.Infrastructure.Data;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = new[]
        {
            Roles.Admin,
            Roles.Agent,
            Roles.User
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
