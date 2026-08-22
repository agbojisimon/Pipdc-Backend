using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Users;

public interface IUserService
{
    Task<Result<PaginatedResult<UserDto>>> GetAllAsync(UserQueryParameters q, CancellationToken ct);
    Task<Result<UserDetailDto>> GetByIdAsync(string userId, CancellationToken ct);
    Task<Result> DeactivateAsync(string userId, CancellationToken ct);
    Task<Result> ActivateAsync(string userId, CancellationToken ct);
}
