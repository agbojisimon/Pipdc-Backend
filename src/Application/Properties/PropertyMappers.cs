using PIPDC.Domain.Entities;

namespace PIPDC.Application.Properties;

public static class PropertyMappers
{
    public static PropertyDto ToDto(this Property property) =>
        new(
            property.Id,
            property.Title,
            property.Description,
            property.Price,
            property.Address,
            property.State,
            property.City,
            property.Bedrooms,
            property.Bathrooms,
            property.SizeInSqM,
            property.PropertyType.ToString(),
            property.ListingType.ToString(),
            property.Status.ToString(),
            property.AgentId,
            property.Agent.User.FullName,
            property.CreatedAt,
            property.UpdatedAt);
}
