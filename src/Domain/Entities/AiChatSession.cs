using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class AiChatSession : BaseEntity
{
    public string? Title { get; set; }
    public string MessagesJson { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public string UserId { get; set; } = string.Empty;

    public AppUser User { get; set; } = null!;
}
