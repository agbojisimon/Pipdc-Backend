using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Auth;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Agents;

public class AgentService(IAppDbContext dbContext, UserManager<AppUser> userManager) : IAgentService
{
    public async Task<Result<PaginatedResult<AgentDto>>> GetAllAsync(AgentQueryParameters q, CancellationToken ct)
    {
        IQueryable<Agent> query = dbContext.Agents;

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.ToLower();
            query = query.Where(a => a.AgencyName.ToLower().Contains(keyword));
        }

        if (q.IsVerified.HasValue)
            query = query.Where(a => a.IsVerified == q.IsVerified.Value);

        var totalCount = await query.CountAsync(ct);

        query = q.SortBy?.ToLower() switch
        {
            "agencyname" => q.SortDescending ? query.OrderByDescending(a => a.AgencyName)
                                             : query.OrderBy(a => a.AgencyName),
            _ => q.SortDescending ? query.OrderByDescending(a => a.CreatedAt)
                                  : query.OrderBy(a => a.CreatedAt)
        };

        var items = await query
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(a => new AgentDto(
                a.Id,
                a.Bio,
                a.AgencyName,
                a.LicenseNumber,
                a.PhoneNumber,
                a.IsVerified,
                a.UserId,
                a.User.Email!,
                a.User.FirstName,
                a.User.LastName,
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(ct);

        return Result<PaginatedResult<AgentDto>>.Success(
            PaginatedResult<AgentDto>.Create(items, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result<AgentDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var agent = await dbContext.Agents
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (agent is null)
            return Result<AgentDto>.Failure(
                Error.NotFound("agent.notfound", $"Agent with id {id} was not found."));

        return Result<AgentDto>.Success(agent.ToDto());
    }

    public async Task<Result<AgentDto>> GetMyProfileAsync(string userId, CancellationToken ct)
    {
        var agent = await dbContext.Agents
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        if (agent is null)
            return Result<AgentDto>.Failure(
                Error.NotFound("agent.notfound", "You do not have an agent profile."));

        return Result<AgentDto>.Success(agent.ToDto());
    }

    public async Task<Result<AgentDto>> CreateAsync(CreateAgentRequest request, CancellationToken ct)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Result<AgentDto>.Failure(
                Error.Conflict("agent.duplicateemail", "A user with this email already exists."));

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result<AgentDto>.Failure(
                Error.Validation("agent.identityfailed", errors));
        }

        await userManager.AddToRoleAsync(user, Roles.Agent);

        var agent = new Agent
        {
            Bio = request.Bio,
            AgencyName = request.AgencyName,
            LicenseNumber = request.LicenseNumber,
            PhoneNumber = request.PhoneNumber,
            IsVerified = false,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync(ct);

        var created = await dbContext.Agents
            .Include(a => a.User)
            .FirstAsync(a => a.Id == agent.Id, ct);

        return Result<AgentDto>.Success(created.ToDto());
    }

    public async Task<Result<AgentDto>> UpdateAsync(int id, UpdateAgentRequest request, CancellationToken ct)
    {
        var agent = await dbContext.Agents
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (agent is null)
            return Result<AgentDto>.Failure(
                Error.NotFound("agent.notfound", $"Agent with id {id} was not found."));

        agent.Bio = request.Bio;
        agent.AgencyName = request.AgencyName;
        agent.LicenseNumber = request.LicenseNumber;
        agent.PhoneNumber = request.PhoneNumber;
        agent.IsVerified = request.IsVerified;
        agent.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result<AgentDto>.Success(agent.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var agent = await dbContext.Agents.FindAsync([id], ct);

        if (agent is null)
            return Result.Failure(
                Error.NotFound("agent.notfound", $"Agent with id {id} was not found."));

        dbContext.Agents.Remove(agent);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Result.Failure(
                Error.Conflict("agent.haslistings", "Cannot delete an agent with active property listings."));
        }

        return Result.Success();
    }
}
