namespace PIPDC.Application.Properties;

public record PropertyDto(
    int Id,
    string Title,
    string Description,
    decimal Price,
    string Address,
    string State,
    string City,
    int? Bedrooms,
    int? Bathrooms,
    double? SizeInSqM,
    string PropertyType,
    string ListingType,
    string Status,
    int AgentId,
    string AgentName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreatePropertyRequest(
    string Title,
    string Description,
    decimal Price,
    string Address,
    string State,
    string City,
    int? Bedrooms,
    int? Bathrooms,
    double? SizeInSqM,
    string PropertyType,
    string ListingType,
    int AgentId);

public record UpdatePropertyRequest(
    string Title,
    string Description,
    decimal Price,
    string Address,
    string State,
    string City,
    int? Bedrooms,
    int? Bathrooms,
    double? SizeInSqM,
    string PropertyType,
    string ListingType,
    int AgentId);
