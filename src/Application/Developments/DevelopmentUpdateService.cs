using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;

namespace PIPDC.Application.Developments;

public class DevelopmentUpdateService(IAppDbContext dbContext) : IDevelopmentUpdateService
{
    public async Task<Result<IReadOnlyList<DevelopmentUpdateDto>>> GetByProjectAsync(int projectId, CancellationToken ct)
    {
        if (!await dbContext.DevelopmentProjects.AnyAsync(p => p.Id == projectId, ct))
            return Result<IReadOnlyList<DevelopmentUpdateDto>>.Failure(
                Error.NotFound("development.notfound", $"Development project with id {projectId} was not found."));

        var updates = await dbContext.DevelopmentUpdates
            .Where(u => u.DevelopmentProjectId == projectId)
            .OrderByDescending(u => u.UpdateDate)
            .ToListAsync(ct);

        var dtos = updates.Select(u => new DevelopmentUpdateDto(
            u.Id, u.Title, u.Description, u.ProgressPercentage,
            u.UpdateDate, u.ImageUrls, u.CreatedAt, u.UpdatedAt)).ToList();

        return Result<IReadOnlyList<DevelopmentUpdateDto>>.Success(dtos);
    }

    public async Task<Result<DevelopmentUpdateDto>> CreateAsync(int projectId, CreateDevelopmentUpdateRequest request, CancellationToken ct)
    {
        if (!await dbContext.DevelopmentProjects.AnyAsync(p => p.Id == projectId, ct))
            return Result<DevelopmentUpdateDto>.Failure(
                Error.NotFound("development.notfound", $"Development project with id {projectId} was not found."));

        var update = new DevelopmentUpdate
        {
            DevelopmentProjectId = projectId,
            Title = request.Title,
            Description = request.Description,
            ProgressPercentage = request.ProgressPercentage,
            UpdateDate = request.UpdateDate ?? DateTime.UtcNow,
            ImageUrls = request.ImageUrls ?? [],
            CreatedAt = DateTime.UtcNow
        };

        dbContext.DevelopmentUpdates.Add(update);
        await dbContext.SaveChangesAsync(ct);

        return Result<DevelopmentUpdateDto>.Success(new DevelopmentUpdateDto(
            update.Id, update.Title, update.Description, update.ProgressPercentage,
            update.UpdateDate, update.ImageUrls, update.CreatedAt, update.UpdatedAt));
    }

    public async Task<Result<DevelopmentUpdateDto>> UpdateAsync(int projectId, int updateId, UpdateDevelopmentUpdateRequest request, CancellationToken ct)
    {
        var update = await dbContext.DevelopmentUpdates
            .FirstOrDefaultAsync(u => u.Id == updateId && u.DevelopmentProjectId == projectId, ct);

        if (update is null)
            return Result<DevelopmentUpdateDto>.Failure(
                Error.NotFound("update.notfound", $"Development update with id {updateId} was not found in project {projectId}."));

        update.Title = request.Title;
        update.Description = request.Description;
        update.ProgressPercentage = request.ProgressPercentage;
        if (request.UpdateDate.HasValue)
            update.UpdateDate = request.UpdateDate.Value;
        update.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result<DevelopmentUpdateDto>.Success(new DevelopmentUpdateDto(
            update.Id, update.Title, update.Description, update.ProgressPercentage,
            update.UpdateDate, update.ImageUrls, update.CreatedAt, update.UpdatedAt));
    }

    public async Task<Result> DeleteAsync(int projectId, int updateId, CancellationToken ct)
    {
        var update = await dbContext.DevelopmentUpdates
            .FirstOrDefaultAsync(u => u.Id == updateId && u.DevelopmentProjectId == projectId, ct);

        if (update is null)
            return Result.Failure(
                Error.NotFound("update.notfound", $"Development update with id {updateId} was not found in project {projectId}."));

        dbContext.DevelopmentUpdates.Remove(update);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
