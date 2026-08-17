using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class Conversation : AuditableEntity
{
    public int EnquiryId { get; set; }
    public string ClientUserId { get; set; } = string.Empty;
    public int AgentId { get; set; }

    // UTC timestamp of the most recent message sent in this conversation.
    // Null until the first message is sent; used to order conversation lists.
    public DateTime? LastMessageAt { get; set; }

    public Enquiry Enquiry { get; set; } = null!;
    public AppUser Client { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = [];
}
