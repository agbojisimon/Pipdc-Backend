using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Entities;

public class DevelopmentUnit : AuditableEntity
{
    public int DevelopmentProjectId { get; set; }
    public string UnitIdentifier { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public DevelopmentUnitStatus Status { get; set; }
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "NGN";
    public string? Description { get; set; }

    public DevelopmentProject Project { get; set; } = null!;
    public ICollection<DevelopmentTracking> TrackedBy { get; set; } = [];
}
