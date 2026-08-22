using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Developments;

public class DevelopmentTrackingService(IAppDbContext dbContext) : IDevelopmentTrackingService
{
    public async Task<Result<PaginatedResult<DevelopmentTrackingDto>>> GetTrackedAsync(string userId, DevelopmentProjectQueryParameters q, CancellationToken ct)
    {
        IQueryable<DevelopmentTracking> query = dbContext.DevelopmentTrackings
            .Where(t => t.UserId == userId && t.Status == DevelopmentTrackingStatus.Following);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Include(t => t.Project)
            .Include(t => t.Unit)
            .ToListAsync(ct);

        var dtos = items.Select(t => new DevelopmentTrackingDto(
            t.Id,
            t.DevelopmentProjectId,
            t.Project.Name,
            t.DevelopmentUnitId,
            t.Unit?.UnitIdentifier,
            t.Status.ToString(),
            t.CreatedAt)).ToList();

        return Result<PaginatedResult<DevelopmentTrackingDto>>.Success(
            PaginatedResult<DevelopmentTrackingDto>.Create(dtos, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result> TrackAsync(string userId, int projectId, int? unitId, CancellationToken ct)
    {
        if (!await dbContext.DevelopmentProjects.AnyAsync(p => p.Id == projectId, ct))
            return Result.Failure(
                Error.NotFound("development.notfound", $"Development project with id {projectId} was not found."));

        if (unitId.HasValue)
        {
            var unit = await dbContext.DevelopmentUnits
                .FirstOrDefaultAsync(u => u.Id == unitId.Value && u.DevelopmentProjectId == projectId, ct);

            if (unit is null)
                return Result.Failure(
                    Error.NotFound("unit.notfound", $"Development unit with id {unitId} was not found in project {projectId}."));
        }

        var existing = await dbContext.DevelopmentTrackings
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DevelopmentProjectId == projectId, ct);

        if (existing is not null)
        {
            if (existing.Status == DevelopmentTrackingStatus.Following)
                return Result.Success();

            existing.Status = DevelopmentTrackingStatus.Following;
            existing.DevelopmentUnitId = unitId;
            await dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }

        dbContext.DevelopmentTrackings.Add(new DevelopmentTracking
        {
            UserId = userId,
            DevelopmentProjectId = projectId,
            DevelopmentUnitId = unitId,
            Status = DevelopmentTrackingStatus.Following,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> StopTrackingAsync(string userId, int projectId, CancellationToken ct)
    {
        var tracking = await dbContext.DevelopmentTrackings
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DevelopmentProjectId == projectId, ct);

        if (tracking is null || tracking.Status == DevelopmentTrackingStatus.Stopped)
            return Result.Success();

        tracking.Status = DevelopmentTrackingStatus.Stopped;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<bool> IsTrackingAsync(string userId, int projectId, CancellationToken ct)
    {
        return await dbContext.DevelopmentTrackings
            .AnyAsync(t => t.UserId == userId && t.DevelopmentProjectId == projectId && t.Status == DevelopmentTrackingStatus.Following, ct);
    }

    public async Task<Result<PaginatedResult<AdminDevelopmentTrackingDto>>> AdminGetAllAsync(DevelopmentTrackingQueryParameters q, CancellationToken ct)
    {
        IQueryable<DevelopmentTracking> query = dbContext.DevelopmentTrackings;

        if (q.ProjectId.HasValue)
            query = query.Where(t => t.DevelopmentProjectId == q.ProjectId.Value);

        if (!string.IsNullOrWhiteSpace(q.UserId))
            query = query.Where(t => t.UserId == q.UserId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Include(t => t.User)
            .Include(t => t.Project)
            .Include(t => t.Unit)
            .ToListAsync(ct);

        var dtos = items.Select(t => new AdminDevelopmentTrackingDto(
            t.Id,
            t.UserId,
            t.User.FullName,
            t.User.Email ?? string.Empty,
            t.DevelopmentProjectId,
            t.Project.Name,
            t.DevelopmentUnitId,
            t.Unit?.UnitIdentifier,
            t.Status.ToString(),
            t.CreatedAt)).ToList();

        return Result<PaginatedResult<AdminDevelopmentTrackingDto>>.Success(
            PaginatedResult<AdminDevelopmentTrackingDto>.Create(dtos, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result<IReadOnlyList<AdminDevelopmentTrackingDto>>> AdminGetByProjectAsync(int projectId, CancellationToken ct)
    {
        var items = await dbContext.DevelopmentTrackings
            .Where(t => t.DevelopmentProjectId == projectId)
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.User)
            .Include(t => t.Unit)
            .ToListAsync(ct);

        var dtos = items.Select(t => new AdminDevelopmentTrackingDto(
            t.Id,
            t.UserId,
            t.User.FullName,
            t.User.Email ?? string.Empty,
            t.DevelopmentProjectId,
            string.Empty,
            t.DevelopmentUnitId,
            t.Unit?.UnitIdentifier,
            t.Status.ToString(),
            t.CreatedAt)).ToList();

        return Result<IReadOnlyList<AdminDevelopmentTrackingDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<AdminDevelopmentTrackingDto>>> AdminGetByUserAsync(string userId, CancellationToken ct)
    {
        var items = await dbContext.DevelopmentTrackings
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.Project)
            .Include(t => t.Unit)
            .ToListAsync(ct);

        var dtos = items.Select(t => new AdminDevelopmentTrackingDto(
            t.Id,
            t.UserId,
            string.Empty,
            string.Empty,
            t.DevelopmentProjectId,
            t.Project.Name,
            t.DevelopmentUnitId,
            t.Unit?.UnitIdentifier,
            t.Status.ToString(),
            t.CreatedAt)).ToList();

        return Result<IReadOnlyList<AdminDevelopmentTrackingDto>>.Success(dtos);
    }

    public async Task<Result> AdminRemoveTrackingAsync(int trackingId, CancellationToken ct)
    {
        var tracking = await dbContext.DevelopmentTrackings
            .FirstOrDefaultAsync(t => t.Id == trackingId, ct);

        if (tracking is null)
            return Result.Failure(
                Error.NotFound("tracking.notfound", $"Development tracking record with id {trackingId} was not found."));

        dbContext.DevelopmentTrackings.Remove(tracking);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> AdminUpdateTrackingStatusAsync(int trackingId, string status, CancellationToken ct)
    {
        var tracking = await dbContext.DevelopmentTrackings
            .FirstOrDefaultAsync(t => t.Id == trackingId, ct);

        if (tracking is null)
            return Result.Failure(
                Error.NotFound("tracking.notfound", $"Development tracking record with id {trackingId} was not found."));

        if (!Enum.TryParse<DevelopmentTrackingStatus>(status, true, out var parsed))
            return Result.Failure(
                Error.Validation("tracking.invalidstatus", $"'{status}' is not a valid tracking status."));

        tracking.Status = parsed;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
