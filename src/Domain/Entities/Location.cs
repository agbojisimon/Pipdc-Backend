using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Entities;

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public LocationType Type { get; set; }
    public int? ParentId { get; set; }
    public Location? Parent { get; set; }
    public ICollection<Location> Children { get; set; } = [];
    public ICollection<Property> Properties { get; set; } = [];
    public ICollection<DevelopmentProject> DevelopmentProjects { get; set; } = [];
}