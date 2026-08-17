using PIPDC.Domain.Entities;

namespace PIPDC.Application.Conversations;

public static class MessageMappers
{
    public static MessageDto ToDto(this Message message) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.Sender.FullName,
            message.Content,
            message.CreatedAt,
            message.ReadAt,
            message.ReadAt is not null);
}
