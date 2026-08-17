using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class Message : BaseEntity
{
    public int ConversationId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    // UTC timestamp of the first time the recipient read this message.
    // Null until read; used to track read/unread state.
    public DateTime? ReadAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public AppUser Sender { get; set; } = null!;
}
