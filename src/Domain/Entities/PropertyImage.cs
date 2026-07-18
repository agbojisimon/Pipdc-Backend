using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class PropertyImage : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public bool IsCover { get; set; }
    public int PropertyId { get; set; }

    public Property Property { get; set; } = null!;
}
