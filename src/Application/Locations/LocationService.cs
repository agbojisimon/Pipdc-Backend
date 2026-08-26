using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Locations;

public class LocationService(IAppDbContext dbContext) : ILocationService
{
    public async Task<Result<IReadOnlyList<LocationDto>>> GetAllAsync(string? type, int? parentId, CancellationToken ct)
    {
        IQueryable<Location> query = dbContext.Locations;

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<LocationType>(type, true, out var parsedType))
            query = query.Where(l => l.Type == parsedType);

        if (parentId.HasValue)
            query = query.Where(l => l.ParentId == parentId);

        var items = await query
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto(
                l.Id,
                l.Name,
                l.Slug,
                l.Type.ToString(),
                l.ParentId,
                l.Parent != null ? l.Parent.Name : null,
                l.Children.Count))
            .ToListAsync(ct);

        return Result<IReadOnlyList<LocationDto>>.Success(items);
    }

    public async Task<Result<LocationDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var l = await dbContext.Locations
            .Include(x => x.Parent)
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (l is null)
            return Result<LocationDto>.Failure(
                Error.NotFound("location.notfound", $"Location with id {id} was not found."));

        return Result<LocationDto>.Success(new LocationDto(
            l.Id,
            l.Name,
            l.Slug,
            l.Type.ToString(),
            l.ParentId,
            l.Parent?.Name,
            l.Children.Count));
    }

    public async Task<Result<IReadOnlyList<LocationDto>>> GetHierarchyAsync(int? stateId, CancellationToken ct)
    {
        IQueryable<Location> query = dbContext.Locations
            .Include(l => l.Children)
                .ThenInclude(c => c.Children)
                    .ThenInclude(a => a.Children);

        if (stateId.HasValue)
            query = query.Where(l => l.Id == stateId && l.Type == LocationType.State);

        var states = await query
            .Where(l => l.Type == LocationType.State)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

        var result = states.Select(s => new LocationDto(
            s.Id,
            s.Name,
            s.Slug,
            s.Type.ToString(),
            s.ParentId,
            null,
            s.Children.Count)).ToList();

        return Result<IReadOnlyList<LocationDto>>.Success(result);
    }

    public async Task<Result<LocationDto>> CreateAsync(CreateLocationRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<LocationType>(request.Type, true, out var locationType))
            return Result<LocationDto>.Failure(
                Error.Validation("location.invalidtype", $"'{request.Type}' is not a valid location type. Use: State, LGA, City, or Area."));

        var slug = Slugify(request.Slug ?? request.Name);

        if (await dbContext.Locations.AnyAsync(l => l.Name == request.Name && l.ParentId == request.ParentId, ct))
            return Result<LocationDto>.Failure(
                Error.Validation("location.duplicate", $"A location named '{request.Name}' already exists at this level."));

        if (await dbContext.Locations.AnyAsync(l => l.Slug == slug, ct))
            slug = await EnsureUniqueSlugAsync(slug, ct);

        var location = new Location
        {
            Name = request.Name,
            Slug = slug,
            Type = locationType,
            ParentId = request.ParentId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync(ct);

        return Result<LocationDto>.Success(new LocationDto(
            location.Id,
            location.Name,
            location.Slug,
            location.Type.ToString(),
            location.ParentId,
            null,
            0));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var location = await dbContext.Locations.FindAsync([id], ct);
        if (location is null)
            return Result.Failure(Error.NotFound("location.notfound", $"Location with id {id} was not found."));

        var hasChildren = await dbContext.Locations.AnyAsync(l => l.ParentId == id, ct);
        if (hasChildren)
            return Result.Failure(Error.Validation("location.haschildren", "Cannot delete a location that has child locations. Delete the children first."));

        var hasProperties = await dbContext.Properties.AnyAsync(p => p.LocationId == id, ct);
        if (hasProperties)
            return Result.Failure(Error.Validation("location.hasreferences", "Cannot delete a location that is assigned to properties. Remove the assignments first."));

        dbContext.Locations.Remove(location);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLower(), "[^a-z0-9]+", "-").Trim('-');
        return slug.Length == 0 ? "location" : slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        var candidate = baseSlug;
        var suffix = 2;
        while (await dbContext.Locations.AnyAsync(l => l.Slug == candidate, ct))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return candidate;
    }
}
