using PIPDC.Domain.Enums;

namespace PIPDC.Application.Properties;

public static class PropertyStatusDisplay
{
    public static string ToFrontend(PropertyStatus status) =>
        status switch
        {
            PropertyStatus.Available => "Available",
            PropertyStatus.Pending => "Pending",
            PropertyStatus.Sold => "Sold",
            PropertyStatus.Rented => "Rented",
            PropertyStatus.Unavailable => "Unavailable",
            _ => status.ToString()
        };

    public static bool TryParse(string? value, out PropertyStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        switch (value.Trim())
        {
            case "Available" or "ForSale" or "For Sale":
                status = PropertyStatus.Available;
                return true;
            case "Pending":
                status = PropertyStatus.Pending;
                return true;
            case "Sold":
                status = PropertyStatus.Sold;
                return true;
            case "Rented" or "Leased":
                status = PropertyStatus.Rented;
                return true;
            case "Unavailable" or "Withdrawn" or "OffMarket" or "Off Market":
                status = PropertyStatus.Unavailable;
                return true;
            default:
                return Enum.TryParse<PropertyStatus>(value, true, out status);
        }
    }
}
