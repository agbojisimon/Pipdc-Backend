using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Entities;

public class Property : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Address { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public double? SizeInSqM { get; set; }
    public PropertyType PropertyType { get; set; }
    public ListingType ListingType { get; set; }
    public PropertyStatus Status { get; set; }
    public int AgentId { get; set; }

    public Agent Agent { get; set; } = null!;
    public ICollection<PropertyImage> PropertyImages { get; set; } = [];
    public ICollection<Enquiry> Enquiries { get; set; } = [];
    public ICollection<SavedProperty> SavedByUsers { get; set; } = [];
    public SaleRecord? SaleRecord { get; set; }
    public LeaseRecord? LeaseRecord { get; set; }
}
