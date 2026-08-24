using System.ComponentModel.DataAnnotations;

namespace PIPDC.Application.Enquiries;

public record EnquiryDto(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    string Message,
    string Status,
    int PropertyId,
    string PropertyTitle,
    string PropertySlug,
    string? UserId,
    int? AgentId,
    string? AgentName,
    DateTime? AgentReadAt,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record AgentEnquirySummaryDto(
    int AgentId,
    string AgentName,
    int TotalEnquiries,
    int UnreadEnquiries,
    DateTime? LatestEnquiryAt);

public record AgentNotifyResultDto(
    int EnquiryId,
    string EnquiryStatus,
    string ClientFullName,
    string ClientEmail,
    string? ClientPhone,
    string ClientMessage,
    int? AgentId,
    string? AgentName,
    string? AgentEmail,
    int PropertyId,
    string PropertyTitle,
    string PropertySlug,
    DateTime? AgentReadAt);

public record CreateEnquiryRequest(
    [Required, MaxLength(4000)] string Message,
    [Range(1, int.MaxValue)] int PropertyId);

public record UpdateEnquiryRequest(
    [Required, MaxLength(200)] string FullName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [MaxLength(20)] string? Phone,
    [Required, MaxLength(4000)] string Message,
    [Required] string Status);
