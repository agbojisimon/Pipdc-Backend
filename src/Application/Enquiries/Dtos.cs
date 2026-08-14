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
    string? UserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateEnquiryRequest(
    [Required, MaxLength(200)] string FullName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [MaxLength(20)] string? Phone,
    [Required, MaxLength(4000)] string Message,
    [Range(1, int.MaxValue)] int PropertyId);

public record UpdateEnquiryRequest(
    [Required, MaxLength(200)] string FullName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [MaxLength(20)] string? Phone,
    [Required, MaxLength(4000)] string Message,
    [Required] string Status);
