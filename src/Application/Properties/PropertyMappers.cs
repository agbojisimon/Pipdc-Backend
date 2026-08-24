using PIPDC.Domain.Entities;

namespace PIPDC.Application.Properties;

public static class PropertyMappers
{
    public static PropertyDto ToDto(this Property property, bool isSaved = false, int enquiryCount = 0)
    {
        var images = property.PropertyImages
            .OrderBy(i => i.DisplayOrder)
            .Select(i => i.Url)
            .ToList();

        var cover = property.PropertyImages
            .Where(i => i.IsCover)
            .Select(i => i.Url)
            .FirstOrDefault()
            ?? images.FirstOrDefault();

        return new PropertyDto(
            property.Id,
            property.Title,
            property.Slug,
            property.Description,
            property.Price,
            property.Currency,
            property.Period,
            PropertyStatusDisplay.ToFrontend(property.Status),
            PropertyTypeDisplay.ToFrontend(property.PropertyType),
            property.PropertyType.ToString(),
            property.ListingType.ToString(),
            property.Bedrooms,
            property.Bathrooms,
            property.Size,
            property.SizeUnit,
            property.LotSize,
            property.YearBuilt,
            property.Address,
            property.City,
            property.Area,
            property.State,
            property.Latitude,
            property.Longitude,
            images,
            cover,
            property.Amenities,
            property.Featured,
            property.AgentId,
            property.Agent?.User.FullName ?? string.Empty,
            property.Agent?.PhotoUrl,
            isSaved,
            enquiryCount,
            property.CreatedAt,
            property.UpdatedAt);
    }
}
