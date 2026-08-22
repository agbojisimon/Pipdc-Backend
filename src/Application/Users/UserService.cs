using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;

namespace PIPDC.Application.Users;

public class UserService(UserManager<AppUser> userManager, IAppDbContext dbContext) : IUserService
{
    public async Task<Result<PaginatedResult<UserDto>>> GetAllAsync(UserQueryParameters q, CancellationToken ct)
    {
        List<AppUser> allUsers;

        if (!string.IsNullOrWhiteSpace(q.Role))
        {
            // Identity stores role membership in a separate table, so filter by role first
            // (before pagination) to keep total counts and pages correct.
            allUsers = ApplyKeywordFilter(
                    (await userManager.GetUsersInRoleAsync(q.Role)).AsQueryable(), q.Keyword)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();
        }
        else
        {
            IQueryable<AppUser> query = ApplyKeywordFilter(userManager.Users, q.Keyword);
            allUsers = await query.OrderByDescending(u => u.CreatedAt).ToListAsync(ct);
        }

        var totalCount = allUsers.Count;

        var page = allUsers
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToList();

        var items = new List<UserDto>(page.Count);
        var agentLookup = await dbContext.Agents
            .Select(a => new { a.UserId, a.Id })
            .ToListAsync(ct);

        foreach (var user in page)
        {
            var roles = await userManager.GetRolesAsync(user);

            var status = user.LockoutEnd is null || user.LockoutEnd < DateTimeOffset.UtcNow
                ? "Active"
                : "Suspended";

            items.Add(new UserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.Email!,
                roles,
                status,
                user.CreatedAt,
                agentLookup.FirstOrDefault(a => a.UserId == user.Id)?.Id));
        }

        return Result<PaginatedResult<UserDto>>.Success(
            PaginatedResult<UserDto>.Create(items, totalCount, q.PageNumber, q.PageSize));
    }

    private static IQueryable<AppUser> ApplyKeywordFilter(IQueryable<AppUser> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return query;

        var search = keyword.ToLower();
        return query.Where(u => u.FirstName.ToLower().Contains(search)
                             || u.LastName.ToLower().Contains(search)
                             || u.Email!.ToLower().Contains(search));
    }

    public async Task<Result<UserDetailDto>> GetByIdAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result<UserDetailDto>.Failure(
                Error.NotFound("user.notfound", $"User with id {userId} was not found."));

        var roles = await userManager.GetRolesAsync(user);

        var status = user.LockoutEnd is null || user.LockoutEnd < DateTimeOffset.UtcNow
            ? "Active"
            : "Suspended";

        var agent = await dbContext.Agents
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        return Result<UserDetailDto>.Success(new UserDetailDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Email!,
            user.PhoneNumber,
            user.CreatedAt,
            roles,
            status,
            agent?.Id,
            agent?.LicenseNumber,
            agent?.AgencyName,
            agent?.IsVerified));
    }

    public async Task<Result> DeactivateAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(
                Error.NotFound("user.notfound", $"User with id {userId} was not found."));

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains("Admin"))
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count <= 1)
                return Result.Failure(
                    Error.Conflict("admin.required", "Cannot deactivate the last admin user."));
        }

        user.LockoutEnd = DateTimeOffset.MaxValue;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(
                Error.Validation("user.updatefailed", errors));
        }

        return Result.Success();
    }

    public async Task<Result> ActivateAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(
                Error.NotFound("user.notfound", $"User with id {userId} was not found."));

        user.LockoutEnd = null;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(
                Error.Validation("user.updatefailed", errors));
        }

        return Result.Success();
    }
}
