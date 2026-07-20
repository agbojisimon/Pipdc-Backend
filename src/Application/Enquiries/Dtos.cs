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
    string FullName,
    string Email,
    string? Phone,
    string Message,
    int PropertyId);

public record UpdateEnquiryRequest(
    string FullName,
    string Email,
    string? Phone,
    string Message,
    string Status);
