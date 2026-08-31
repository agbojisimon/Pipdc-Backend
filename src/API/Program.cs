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
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, JwtSubUserIdProvider>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});
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

// Security headers: HSTS (https-only responses) + hardening headers for every API
// response. They are set for every request so error paths are covered too.
app.UseHsts();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    await next();
});

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
