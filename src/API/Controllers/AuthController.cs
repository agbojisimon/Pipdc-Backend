using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PIPDC.Application.Auth;
using PIPDC.Domain.Auth;
using PIPDC.Domain.Entities;
using PIPDC.Infrastructure.Data;

namespace PIPDC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ITokenService tokenService,
    AppDbContext dbContext,
    IOptions<JwtSettings> jwtOptions) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return BadRequest(new { error = "Email already exists." });

        var user = new AppUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await userManager.AddToRoleAsync(user, Roles.User);
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { error = "Invalid credentials." });

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateAccessToken(user.Id, user.Email!, user.FullName, roles);

        var refreshToken = tokenService.CreateRefreshToken();
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        });
        await dbContext.SaveChangesAsync();

        return Ok(new AuthResponse(
            user.Id,
            user.Email!,
            roles,
            token,
            expiresAt,
            refreshToken,
            DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (storedToken is null)
            return Unauthorized(new { error = "Invalid refresh token." });

        if (!storedToken.IsActive)
        {
            if (storedToken.Revoked is not null)
                await RevokeAllActiveTokensAsync(storedToken.UserId);

            return Unauthorized(new { error = "Refresh token is no longer active." });
        }

        storedToken.Revoked = DateTime.UtcNow;

        var newRefreshToken = tokenService.CreateRefreshToken();
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = storedToken.UserId,
            CreatedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        });
        storedToken.ReplacedByToken = newRefreshToken;
        await dbContext.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(storedToken.UserId);
        var roles = await userManager.GetRolesAsync(user!);
        var (token, expiresAt) = tokenService.CreateAccessToken(user!.Id, user.Email!, user.FullName, roles);

        return Ok(new AuthResponse(
            user.Id,
            user.Email!,
            roles,
            token,
            expiresAt,
            newRefreshToken,
            DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (storedToken is null || !storedToken.IsActive)
            return NotFound(new { error = "Token not found or already inactive." });

        storedToken.Revoked = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Ok();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("add-role")]
    public async Task<IActionResult> AddRole([FromBody] AddRoleRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return BadRequest(new { error = "User not found." });

        if (!await roleManager.RoleExistsAsync(request.Role))
            return BadRequest(new { error = "Role does not exist." });

        await userManager.AddToRoleAsync(user, request.Role);
        return Ok();
    }

    private async Task RevokeAllActiveTokensAsync(string userId)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.Revoked == null && t.Expires > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.Revoked = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
    }
}
