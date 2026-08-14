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
}
