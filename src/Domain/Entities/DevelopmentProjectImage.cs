using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class DevelopmentProjectImage : BaseEntity
{
    public int DevelopmentProjectId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public bool IsCover { get; set; }
    public int DisplayOrder { get; set; }

    public DevelopmentProject Project { get; set; } = null!;
}
