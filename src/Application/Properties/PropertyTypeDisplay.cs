using PIPDC.Domain.Enums;

namespace PIPDC.Application.Properties;

public static class PropertyTypeDisplay
{
    private static readonly IReadOnlyDictionary<PropertyType, string> FrontendNames =
        new Dictionary<PropertyType, string>
        {
            [PropertyType.Residential] = "Residential",
            [PropertyType.Commercial] = "Commercial",
            [PropertyType.Land] = "Land",
            [PropertyType.Industrial] = "Industrial",
            [PropertyType.Mixed] = "Mixed",
            [PropertyType.DetachedHouse] = "Detached House",
            [PropertyType.SemiDetached] = "Semi-Detached",
            [PropertyType.Terrace] = "Terrace",
            [PropertyType.Apartment] = "Apartment",
            [PropertyType.Penthouse] = "Penthouse",
            [PropertyType.Villa] = "Villa",
            [PropertyType.Mansion] = "Mansion",
            [PropertyType.Townhouse] = "Townhouse",
        };

    public static string ToFrontend(PropertyType type) =>
        FrontendNames.TryGetValue(type, out var name) ? name : type.ToString();

    public static bool TryParse(string? value, out PropertyType type)
    {
        type = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (var (key, name) in FrontendNames)
        {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
            {
                type = key;
                return true;
            }
        }

        return Enum.TryParse<PropertyType>(value, true, out type);
    }
}
