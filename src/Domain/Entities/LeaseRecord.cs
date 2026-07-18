using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class LeaseRecord : AuditableEntity
{
    public string TenantName { get; set; } = string.Empty;
    public string TenantContact { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public DateTime LeaseStartDate { get; set; }
    public DateTime LeaseEndDate { get; set; }
    public string? Notes { get; set; }
    public int PropertyId { get; set; }

    public Property Property { get; set; } = null!;
}
