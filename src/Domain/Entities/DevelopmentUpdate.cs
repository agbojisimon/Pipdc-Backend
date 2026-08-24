using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class DevelopmentUpdate : AuditableEntity
{
    public int DevelopmentProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? ProgressPercentage { get; set; }
    public DateTime UpdateDate { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    public List<string> ImagePublicIds { get; set; } = [];

    public DevelopmentProject Project { get; set; } = null!;
}
