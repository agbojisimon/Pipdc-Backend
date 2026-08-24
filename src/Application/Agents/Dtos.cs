using System.ComponentModel.DataAnnotations;

namespace PIPDC.Application.Agents;

public record AgentDto(
    int Id,
    string? Bio,
    string? Title,
    string? Photo,
    string? PhotoPublicId,
    string Agency,
    string? LicenseNumber,
    string Phone,
    bool Verified,
    string FullName,
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int PropertyCount);

public record CreateAgentRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [MaxLength(100)] string? Title,
    [MaxLength(500)] string? PhotoUrl,
    [MaxLength(200)] string? PhotoPublicId,
    [MaxLength(4000)] string? Bio,
    [Required, MaxLength(200)] string AgencyName,
    [MaxLength(100)] string? LicenseNumber,
    [Required, MaxLength(20)] string PhoneNumber);

public record UpdateAgentRequest(
    [MaxLength(100)] string? Title,
    [MaxLength(500)] string? PhotoUrl,
    [MaxLength(200)] string? PhotoPublicId,
    [MaxLength(4000)] string? Bio,
    [Required, MaxLength(200)] string AgencyName,
    [MaxLength(100)] string? LicenseNumber,
    [Required, MaxLength(20)] string PhoneNumber,
    bool IsVerified);

public record AgentSummaryDto(
    int Id,
    string? Bio,
    string? Title,
    string? Photo,
    string? PhotoPublicId,
    string Agency,
    string? LicenseNumber,
    string Phone,
    bool Verified,
    string FullName,
    string UserId,
    string Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int PropertyCount,
    int EnquiryCount,
    int ConversationCount);
