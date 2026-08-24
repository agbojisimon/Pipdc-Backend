using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.SavedProperties;

public interface ISavedPropertyService
{
    Task<Result<PaginatedResult<SavedPropertyDto>>> GetSavedAsync(string userId, SavedPropertyQueryParameters q, CancellationToken ct);
    Task<Result<IReadOnlyList<int>>> GetSavedIdsAsync(string userId, CancellationToken ct);
    Task<Result> SaveAsync(string userId, int propertyId, CancellationToken ct);
    Task<Result> UnsaveAsync(string userId, int propertyId, CancellationToken ct);
}
