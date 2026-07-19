namespace PIPDC.Application.Agents;

public record AgentDto(
    int Id,
    string? Bio,
    string AgencyName,
    string? LicenseNumber,
    string PhoneNumber,
    bool IsVerified,
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateAgentRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Bio,
    string AgencyName,
    string? LicenseNumber,
    string PhoneNumber);

public record UpdateAgentRequest(
    string? Bio,
    string AgencyName,
    string? LicenseNumber,
    string PhoneNumber,
    bool IsVerified);
