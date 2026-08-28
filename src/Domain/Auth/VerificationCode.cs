using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Auth;

public class VerificationCode : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public VerificationPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int Attempts { get; set; }

    public bool IsActive(DateTime utcNow) =>
        !IsUsed && RevokedAt is null && ExpiresAt > utcNow;
}