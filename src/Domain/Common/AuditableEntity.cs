namespace PIPDC.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime? UpdatedAt { get; set; }
}
