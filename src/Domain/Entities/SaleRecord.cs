using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class SaleRecord : AuditableEntity
{
    public decimal SalePrice { get; set; }
    public DateTime SaleDate { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerContact { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int PropertyId { get; set; }

    public Property Property { get; set; } = null!;
}
