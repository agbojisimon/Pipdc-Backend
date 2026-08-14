using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Users;

public interface IUserService
{
    Task<Result<PaginatedResult<UserDto>>> GetAllAsync(UserQueryParameters q, CancellationToken ct);
}
