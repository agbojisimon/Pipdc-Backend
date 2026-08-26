using PIPDC.Domain.Common;

namespace PIPDC.Application.Auth;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task<Result> RevokeAsync(RevokeRequest request, CancellationToken ct);
    Task<Result<CurrentUserDto>> GetMeAsync(string userId, CancellationToken ct);
    Task<Result<CurrentUserDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct);
    Task<Result> ForgotPasswordAsync(string email, CancellationToken ct);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct);
    Task<Result> AddRoleAsync(AddRoleRequest request, CancellationToken ct);
    Task<Result> RemoveRoleAsync(RemoveRoleRequest request, CancellationToken ct);
}
