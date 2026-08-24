using PIPDC.Application.Properties;

namespace PIPDC.Application.SavedProperties;

public record SavedPropertyDto(
    PropertyDto Property,
    DateTime SavedAt);
