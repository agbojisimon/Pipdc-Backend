using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using PIPDC.API.Extensions;
using PIPDC.Application.Auth;

namespace PIPDC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request, ct);
        return result.ToActionResult();
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken ct)
    {
        var result = await authService.RevokeAsync(request, ct);
        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await authService.ForgotPasswordAsync(request.Email, ct);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId is null)
            return Unauthorized();

        var result = await authService.GetMeAsync(userId, ct);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId is null)
            return Unauthorized();

        var result = await authService.UpdateProfileAsync(userId, request, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("add-role")]
    public async Task<IActionResult> AddRole([FromBody] AddRoleRequest request, CancellationToken ct)
    {
        var result = await authService.AddRoleAsync(request, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("remove-role")]
    public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest request, CancellationToken ct)
    {
        var result = await authService.RemoveRoleAsync(request, ct);
        return result.ToActionResult();
    }
}
