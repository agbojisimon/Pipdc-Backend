using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Application.Properties;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;

namespace PIPDC.Application.SavedProperties;

public class SavedPropertyService(IAppDbContext dbContext) : ISavedPropertyService
{
    public async Task<Result<PaginatedResult<SavedPropertyDto>>> GetSavedAsync(string userId, SavedPropertyQueryParameters q, CancellationToken ct)
    {
        var query = dbContext.SavedProperties
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var savedRows = await query
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(s => new { s.PropertyId, SavedAt = s.CreatedAt })
            .ToListAsync(ct);

        var propertyIds = savedRows.Select(s => s.PropertyId).ToList();
        var savedAtMap = savedRows.ToDictionary(s => s.PropertyId, s => s.SavedAt);

        var properties = await dbContext.Properties
            .Include(p => p.Agent)
                .ThenInclude(a => a!.User)
            .Include(p => p.PropertyImages)
            .Where(p => propertyIds.Contains(p.Id))
            .ToListAsync(ct);

        var propertyMap = properties.ToDictionary(p => p.Id);

        var counts = await dbContext.Enquiries
            .Where(e => propertyIds.Contains(e.PropertyId))
            .GroupBy(e => e.PropertyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var dtos = savedRows
            .Where(s => propertyMap.ContainsKey(s.PropertyId))
            .Select(s => new SavedPropertyDto(
                propertyMap[s.PropertyId].ToDto(isSaved: true, enquiryCount: counts.GetValueOrDefault(s.PropertyId)),
                s.SavedAt))
            .ToList();

        return Result<PaginatedResult<SavedPropertyDto>>.Success(
            PaginatedResult<SavedPropertyDto>.Create(dtos, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result<IReadOnlyList<int>>> GetSavedIdsAsync(string userId, CancellationToken ct)
    {
        var ids = await dbContext.SavedProperties
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.PropertyId)
            .ToListAsync(ct);

        return Result<IReadOnlyList<int>>.Success(ids);
    }

    public async Task<Result> SaveAsync(string userId, int propertyId, CancellationToken ct)
    {
        if (!await dbContext.Properties.AnyAsync(p => p.Id == propertyId, ct))
            return Result.Failure(
                Error.NotFound("property.notfound", $"Property with id {propertyId} was not found."));

        if (await dbContext.SavedProperties.AnyAsync(s => s.UserId == userId && s.PropertyId == propertyId, ct))
            return Result.Success();

        dbContext.SavedProperties.Add(new SavedProperty
        {
            UserId = userId,
            PropertyId = propertyId,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UnsaveAsync(string userId, int propertyId, CancellationToken ct)
    {
        var saved = await dbContext.SavedProperties
            .FirstOrDefaultAsync(s => s.UserId == userId && s.PropertyId == propertyId, ct);

        if (saved is null)
            return Result.Success();

        dbContext.SavedProperties.Remove(saved);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
