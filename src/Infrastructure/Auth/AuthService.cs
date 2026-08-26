using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PIPDC.Application.Auth;
using PIPDC.Domain.Auth;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;
using PIPDC.Infrastructure.Data;

namespace PIPDC.Infrastructure.Auth;

public class AuthService(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ITokenService tokenService,
    AppDbContext dbContext,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Result.Failure(Error.Conflict("EMAIL_EXISTS", "Email already exists."));

        var user = new AppUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Result.Failure(Error.Validation("REGISTRATION_FAILED",
                string.Join("; ", result.Errors.Select(e => e.Description))));

        await userManager.AddToRoleAsync(user, Roles.User);
        return Result.Success();
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Result<AuthResponse>.Failure(Error.Unauthorized("INVALID_CREDENTIALS", "Invalid credentials."));

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (storedToken is null)
            return Result<AuthResponse>.Failure(Error.Unauthorized("INVALID_REFRESH_TOKEN", "Invalid refresh token."));

        if (!storedToken.IsActive)
        {
            if (storedToken.Revoked is not null)
                await RevokeAllActiveTokensAsync(storedToken.UserId, ct);

            return Result<AuthResponse>.Failure(Error.Unauthorized("REFRESH_TOKEN_INACTIVE", "Refresh token is no longer active."));
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
        await dbContext.SaveChangesAsync(ct);

        var user = await userManager.FindByIdAsync(storedToken.UserId);
        return await BuildAuthResponseAsync(user!, newRefreshToken, ct);
    }

    public async Task<Result> RevokeAsync(RevokeRequest request, CancellationToken ct)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (storedToken is null || !storedToken.IsActive)
            return Result.Failure(Error.NotFound("TOKEN_NOT_FOUND", "Token not found or already inactive."));

        storedToken.Revoked = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<CurrentUserDto>> GetMeAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result<CurrentUserDto>.Failure(Error.NotFound("USER_NOT_FOUND", "User not found."));

        var roles = await userManager.GetRolesAsync(user);

        return Result<CurrentUserDto>.Success(new CurrentUserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.FullName,
            roles));
    }

    public async Task<Result<CurrentUserDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result<CurrentUserDto>.Failure(Error.NotFound("USER_NOT_FOUND", "User not found."));

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        await dbContext.SaveChangesAsync(ct);

        var roles = await userManager.GetRolesAsync(user);

        return Result<CurrentUserDto>.Success(new CurrentUserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.FullName,
            roles));
    }

    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.Success();

        // Email delivery is intentionally not implemented yet. The endpoint exists so the
        // frontend can validate the flow; password reset links will be wired to email later.
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(Error.NotFound("USER_NOT_FOUND", "User not found."));

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(Error.Validation("PASSWORD_CHANGE_FAILED",
                string.Join("; ", result.Errors.Select(e => e.Description))));

        return Result.Success();
    }

    public async Task<Result> AddRoleAsync(AddRoleRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result.Failure(Error.NotFound("USER_NOT_FOUND", "User not found."));

        if (!await roleManager.RoleExistsAsync(request.Role))
            return Result.Failure(Error.Validation("ROLE_NOT_FOUND", "Role does not exist."));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        if (string.Equals(request.Role, Roles.Agent, StringComparison.OrdinalIgnoreCase))
        {
            var agent = await dbContext.Agents.FirstOrDefaultAsync(a => a.UserId == user.Id, ct);
            if (agent is null)
            {
                dbContext.Agents.Add(new Agent
                {
                    Bio = null,
                    Title = "Agent",
                    AgencyName = "PIPDC Agency",
                    LicenseNumber = null,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    IsVerified = false,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync(ct);
            }
        }

        var addResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
        {
            await transaction.RollbackAsync(ct);
            return Result.Failure(Error.Validation("ROLE_ADD_FAILED",
                string.Join("; ", addResult.Errors.Select(e => e.Description))));
        }

        await transaction.CommitAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(RemoveRoleRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result.Failure(Error.NotFound("USER_NOT_FOUND", "User not found."));

        if (!await roleManager.RoleExistsAsync(request.Role))
            return Result.Failure(Error.Validation("ROLE_NOT_FOUND", "Role does not exist."));

        if (string.Equals(request.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1 && await userManager.IsInRoleAsync(user, Roles.Admin))
                return Result.Failure(Error.Conflict("ADMIN_REQUIRED", "You cannot remove the last admin user."));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        if (string.Equals(request.Role, Roles.Agent, StringComparison.OrdinalIgnoreCase))
        {
            var agent = await dbContext.Agents
                .Include(a => a.Properties)
                .FirstOrDefaultAsync(a => a.UserId == user.Id, ct);

            if (agent is not null)
            {
                if (agent.Properties.Count > 0)
                {
                    var adminAgent = await GetOrCreateAdminAgentAsync(ct);
                    if (adminAgent.IsFailure)
                    {
                        await transaction.RollbackAsync(ct);
                        return Result.Failure(adminAgent.Error);
                    }

                    foreach (var property in agent.Properties)
                        property.AgentId = adminAgent.Value.Id;

                    await dbContext.SaveChangesAsync(ct);
                }

                dbContext.Agents.Remove(agent);
                await dbContext.SaveChangesAsync(ct);
            }
        }

        var result = await userManager.RemoveFromRoleAsync(user, request.Role);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(ct);
            return Result.Failure(Error.Validation("ROLE_REMOVE_FAILED",
                string.Join("; ", result.Errors.Select(e => e.Description))));
        }

        await transaction.CommitAsync(ct);
        return Result.Success();
    }

    private async Task<Result<AuthResponse>> BuildAuthResponseAsync(AppUser user, CancellationToken ct)
    {
        var refreshToken = tokenService.CreateRefreshToken();
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        });
        await dbContext.SaveChangesAsync(ct);

        return await BuildAuthResponseAsync(user, refreshToken, ct);
    }

    private async Task<Result<AuthResponse>> BuildAuthResponseAsync(AppUser user, string refreshToken, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateAccessToken(user.Id, user.Email!, user.FullName, roles);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.Email!,
            roles,
            token,
            expiresAt,
            refreshToken,
            DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)));
    }

    private async Task<Result<Agent>> GetOrCreateAdminAgentAsync(CancellationToken ct)
    {
        var admin = (await userManager.GetUsersInRoleAsync(Roles.Admin)).FirstOrDefault();
        if (admin is null)
            return Result<Agent>.Failure(Error.Conflict(
                "ADMIN_REQUIRED", "No admin user exists to transfer the properties to."));

        var agent = await dbContext.Agents.FirstOrDefaultAsync(a => a.UserId == admin.Id, ct);
        if (agent is not null)
            return Result<Agent>.Success(agent);

        agent = new Agent
        {
            Title = "Administrator",
            AgencyName = "PIPDC Administration",
            PhoneNumber = admin.PhoneNumber ?? string.Empty,
            IsVerified = false,
            UserId = admin.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync(ct);

        return Result<Agent>.Success(agent);
    }

    private async Task RevokeAllActiveTokensAsync(string userId, CancellationToken ct)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.Revoked == null && t.Expires > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.Revoked = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
