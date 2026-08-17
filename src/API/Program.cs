using PIPDC.API.Extensions;
using PIPDC.API.Hubs;
using PIPDC.Application;
using PIPDC.Infrastructure.Data;
using PIPDC.Infrastructure;
using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, JwtSubUserIdProvider>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Return automatic model-state validation failures (from DataAnnotations on request
// DTOs) in the same { code, message, type } error shape the rest of the API uses.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(ms => ms.Value?.Errors.Count > 0)
            .Select(ms => $"{ms.Key}: {ms.Value!.Errors[0].ErrorMessage}");

        var message = string.Join("; ", errors);

        var error = new Error(
            "validation.requestinvalid",
            string.IsNullOrWhiteSpace(message) ? "The request is invalid." : message,
            ErrorType.Validation);

        return new BadRequestObjectResult(error);
    };
});

var app = builder.Build();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.MigrateAsync();
await RoleSeeder.SeedAsync(scope.ServiceProvider);

if (app.Environment.IsDevelopment())
{
    await DevelopmentSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<MessagingHub>("/hubs/messaging");

app.Run();
