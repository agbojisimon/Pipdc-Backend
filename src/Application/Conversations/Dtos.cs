using System.ComponentModel.DataAnnotations;

namespace PIPDC.Application.Conversations;

public record ConversationClientDto(
    string UserId,
    string FullName,
    string Email);

public record ConversationAgentDto(
    int? AgentId,
    string FullName,
    string AgencyName,
    string? PhotoUrl);

public record ConversationPropertyDto(
    int PropertyId,
    string Title,
    string Slug);

public record ConversationDto(
    int Id,
    int EnquiryId,
    ConversationClientDto Client,
    ConversationAgentDto Agent,
    ConversationPropertyDto Property,
    DateTime? LastMessageAt,
    int MessageCount,
    int UnreadCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record MessageDto(
    int Id,
    int ConversationId,
    string SenderUserId,
    string SenderName,
    string Content,
    DateTime CreatedAt,
    DateTime? ReadAt,
    bool IsRead);

public record SendMessageRequest(
    [Required, MaxLength(4000)] string Content);

public record EnquiryConversationStateDto(
    int EnquiryId,
    ConversationDto? Conversation,
    ConversationClientDto Client,
    ConversationAgentDto Agent,
    ConversationPropertyDto Property);

public record FirstMessageResultDto(
    ConversationDto Conversation,
    MessageDto Message);
