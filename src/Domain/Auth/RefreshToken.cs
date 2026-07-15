using PIPDC.Domain.Common;

namespace PIPDC.Domain.Auth;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public DateTime? Revoked { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsActive => Revoked is null && DateTime.UtcNow < Expires;
}
