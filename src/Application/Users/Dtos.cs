namespace PIPDC.Application.Users;

public record UserDto(
    string Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    IEnumerable<string> Roles,
    string Status,
    DateTime CreatedAt,
    int? AgentId);

public record UserDetailDto(
    string Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateTime CreatedAt,
    IEnumerable<string> Roles,
    string Status,
    int? AgentId,
    string? AgentLicenseNumber,
    string? AgentAgencyName,
    bool? AgentIsVerified);

public class UserQueryParameters
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }

    private int _pageNumber = 1;
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 50 ? 10 : value;
    }
}
