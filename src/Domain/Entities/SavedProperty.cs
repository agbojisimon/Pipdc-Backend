using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class SavedProperty : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int PropertyId { get; set; }

    public AppUser User { get; set; } = null!;
    public Property Property { get; set; } = null!;
}
