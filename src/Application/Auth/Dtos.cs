using System.ComponentModel.DataAnnotations;

namespace PIPDC.Application.Auth;

public record RegisterRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8)] string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
public record RefreshRequest(string RefreshToken);
public record RevokeRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record AddRoleRequest(string Email, string Role);
public record RemoveRoleRequest(string Email, string Role);

public record UpdateProfileRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [MaxLength(20)] string? PhoneNumber);
public record CurrentUserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string FullName,
    IEnumerable<string> Roles);
public record AuthResponse(
    string UserId,
    string Email,
    IEnumerable<string> Roles,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
