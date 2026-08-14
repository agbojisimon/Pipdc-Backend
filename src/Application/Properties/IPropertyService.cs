using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Properties;

public interface IPropertyService
{
    Task<Result<PaginatedResult<PropertyDto>>> GetAllAsync(PropertyQueryParameters queryParams, string? currentUserId, CancellationToken ct);
    Task<Result<PropertyDto>> GetByIdAsync(int id, string? currentUserId, CancellationToken ct);
    Task<Result<PropertyDto>> GetBySlugAsync(string slug, string? currentUserId, CancellationToken ct);
    Task<Result<IReadOnlyList<PropertyDto>>> GetFeaturedAsync(string? currentUserId, CancellationToken ct);
    Task<Result<IReadOnlyList<PropertyDto>>> GetSimilarAsync(int id, string? currentUserId, CancellationToken ct);
    Task<Result<PropertyDto>> CreateAsync(CreatePropertyRequest request, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result<PropertyDto>> UpdateAsync(int id, UpdatePropertyRequest request, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result<PropertyDto>> SetFeaturedAsync(int id, bool featured, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result> DeleteAsync(int id, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
}
