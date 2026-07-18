using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Entities;

public class Enquiry : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Message { get; set; } = string.Empty;
    public EnquiryStatus Status { get; set; }
    public int PropertyId { get; set; }
    public string? UserId { get; set; }

    public Property Property { get; set; } = null!;
    public AppUser? User { get; set; }
}
