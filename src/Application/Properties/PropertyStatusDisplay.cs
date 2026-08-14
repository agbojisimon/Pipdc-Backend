using PIPDC.Domain.Enums;

namespace PIPDC.Application.Properties;

public static class PropertyStatusDisplay
{
    public const string ForSale = "For Sale";
    public const string ForLease = "For Lease";
    public const string Sold = "Sold";
    public const string OffMarket = "Off Market";

    public static string ToFrontend(PropertyStatus status, ListingType listingType) =>
        status switch
        {
            PropertyStatus.Sold => Sold,
            PropertyStatus.Withdrawn or PropertyStatus.Leased => OffMarket,
            _ => listingType == ListingType.ForLease ? ForLease : ForSale
        };

    public static bool TryParse(string? value, out PropertyStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        switch (value.Trim())
        {
            case ForSale or "ForSale" or "Available" or "Pending":
                status = PropertyStatus.Available;
                return true;
            case ForLease or "ForLease":
                status = PropertyStatus.Available;
                return true;
            case Sold:
                status = PropertyStatus.Sold;
                return true;
            case OffMarket or "Withdrawn" or "Leased":
                status = PropertyStatus.Withdrawn;
                return true;
            default:
                return Enum.TryParse<PropertyStatus>(value, true, out status);
        }
    }
}
