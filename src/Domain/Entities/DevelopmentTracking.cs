using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Entities;

public class DevelopmentTracking : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int DevelopmentProjectId { get; set; }
    public int? DevelopmentUnitId { get; set; }
    public DevelopmentTrackingStatus Status { get; set; } = DevelopmentTrackingStatus.Following;

    public AppUser User { get; set; } = null!;
    public DevelopmentProject Project { get; set; } = null!;
    public DevelopmentUnit? Unit { get; set; }
}
