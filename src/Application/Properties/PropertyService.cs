using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Properties;

public class PropertyService(IAppDbContext dbContext) : IPropertyService
{
    public async Task<Result<PaginatedResult<PropertyDto>>> GetAllAsync(PropertyQueryParameters q, CancellationToken ct)
    {
        IQueryable<Property> query = dbContext.Properties;

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(keyword)
                                  || p.Description.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(q.City))
        {
            var city = q.City.ToLower();
            query = query.Where(p => p.City.ToLower().Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(q.State))
        {
            var state = q.State.ToLower();
            query = query.Where(p => p.State.ToLower().Contains(state));
        }

        if (q.MinPrice.HasValue)
            query = query.Where(p => p.Price >= q.MinPrice.Value);

        if (q.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= q.MaxPrice.Value);

        if (q.Bedrooms.HasValue)
            query = query.Where(p => p.Bedrooms == q.Bedrooms.Value);

        if (Enum.TryParse<PropertyType>(q.PropertyType, true, out var propertyType))
            query = query.Where(p => p.PropertyType == propertyType);

        if (Enum.TryParse<ListingType>(q.ListingType, true, out var listingType))
            query = query.Where(p => p.ListingType == listingType);

        if (Enum.TryParse<PropertyStatus>(q.Status, true, out var status))
            query = query.Where(p => p.Status == status);

        var totalCount = await query.CountAsync(ct);

        query = q.SortBy?.ToLower() switch
        {
            "price" => q.SortDescending ? query.OrderByDescending(p => p.Price)
                                        : query.OrderBy(p => p.Price),
            "title" => q.SortDescending ? query.OrderByDescending(p => p.Title)
                                        : query.OrderBy(p => p.Title),
            _ => q.SortDescending ? query.OrderByDescending(p => p.CreatedAt)
                                  : query.OrderBy(p => p.CreatedAt)
        };

        var items = await query
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(p => new PropertyDto(
                p.Id,
                p.Title,
                p.Description,
                p.Price,
                p.Address,
                p.State,
                p.City,
                p.Bedrooms,
                p.Bathrooms,
                p.SizeInSqM,
                p.PropertyType.ToString(),
                p.ListingType.ToString(),
                p.Status.ToString(),
                p.AgentId,
                p.Agent.User.FullName,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(ct);

        return Result<PaginatedResult<PropertyDto>>.Success(
            PaginatedResult<PropertyDto>.Create(items, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result<PropertyDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var property = await dbContext.Properties
            .Include(p => p.Agent)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (property is null)
            return Result<PropertyDto>.Failure(
                Error.NotFound("property.notfound", $"Property with id {id} was not found."));

        return Result<PropertyDto>.Success(property.ToDto());
    }

    public async Task<Result<PropertyDto>> CreateAsync(CreatePropertyRequest request, CancellationToken ct)
    {
        if (!await dbContext.Agents.AnyAsync(a => a.Id == request.AgentId, ct))
            return Result<PropertyDto>.Failure(
                Error.Validation("property.invalidagent", $"Agent with id {request.AgentId} does not exist."));

        if (!Enum.TryParse<PropertyType>(request.PropertyType, true, out var propertyType))
            return Result<PropertyDto>.Failure(
                Error.Validation("property.invalidpropertytype", $"'{request.PropertyType}' is not a valid property type."));

        if (!Enum.TryParse<ListingType>(request.ListingType, true, out var listingType))
            return Result<PropertyDto>.Failure(
                Error.Validation("property.invalidlistingtype", $"'{request.ListingType}' is not a valid listing type."));

        var property = new Property
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Address = request.Address,
            State = request.State,
            City = request.City,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            SizeInSqM = request.SizeInSqM,
            PropertyType = propertyType,
            ListingType = listingType,
            Status = PropertyStatus.Available,
            AgentId = request.AgentId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(ct);

        var created = await dbContext.Properties
            .Include(p => p.Agent)
                .ThenInclude(a => a.User)
            .FirstAsync(p => p.Id == property.Id, ct);

        return Result<PropertyDto>.Success(created.ToDto());
    }

    public async Task<Result<PropertyDto>> UpdateAsync(int id, UpdatePropertyRequest request, CancellationToken ct)
    {
        var property = await dbContext.Properties
            .Include(p => p.Agent)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (property is null)
            return Result<PropertyDto>.Failure(
                Error.NotFound("property.notfound", $"Property with id {id} was not found."));

        if (!await dbContext.Agents.AnyAsync(a => a.Id == request.AgentId, ct))
            return Result<PropertyDto>.Failure(
                Error.Validation("property.invalidagent", $"Agent with id {request.AgentId} does not exist."));

        if (!Enum.TryParse<PropertyType>(request.PropertyType, true, out var propertyType))
            return Result<PropertyDto>.Failure(
                Error.Validation("property.invalidpropertytype", $"'{request.PropertyType}' is not a valid property type."));

        if (!Enum.TryParse<ListingType>(request.ListingType, true, out var listingType))
            return Result<PropertyDto>.Failure(
                Error.Validation("property.invalidlistingtype", $"'{request.ListingType}' is not a valid listing type."));

        property.Title = request.Title;
        property.Description = request.Description;
        property.Price = request.Price;
        property.Address = request.Address;
        property.State = request.State;
        property.City = request.City;
        property.Bedrooms = request.Bedrooms;
        property.Bathrooms = request.Bathrooms;
        property.SizeInSqM = request.SizeInSqM;
        property.PropertyType = propertyType;
        property.ListingType = listingType;
        property.AgentId = request.AgentId;
        property.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        var updated = await dbContext.Properties
            .Include(p => p.Agent)
                .ThenInclude(a => a.User)
            .FirstAsync(p => p.Id == id, ct);

        return Result<PropertyDto>.Success(updated.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var property = await dbContext.Properties.FindAsync([id], ct);

        if (property is null)
            return Result.Failure(
                Error.NotFound("property.notfound", $"Property with id {id} was not found."));

        dbContext.Properties.Remove(property);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Result.Failure(
                Error.Conflict("property.hasrecords", "Cannot delete a property with an existing sale or lease record."));
        }

        return Result.Success();
    }
}
