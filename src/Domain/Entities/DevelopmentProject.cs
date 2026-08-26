using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Entities;

public class DevelopmentProject : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int? LocationRefId { get; set; }
    public Location? LocationRef { get; set; }
    public string? Developer { get; set; }
    public DevelopmentProjectStatus Status { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public int ProgressPercentage { get; set; }
    public bool Featured { get; set; }

    public ICollection<DevelopmentUnit> Units { get; set; } = [];
    public ICollection<DevelopmentUpdate> Updates { get; set; } = [];
    public ICollection<DevelopmentProjectImage> Images { get; set; } = [];
    public ICollection<DevelopmentTracking> TrackedBy { get; set; } = [];
}
