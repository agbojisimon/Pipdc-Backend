using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Locations;

public interface ILocationService
{
    Task<Result<IReadOnlyList<LocationDto>>> GetAllAsync(string? type, int? parentId, CancellationToken ct);
    Task<Result<LocationDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<IReadOnlyList<LocationDto>>> GetHierarchyAsync(int? stateId, CancellationToken ct);
    Task<Result<LocationDto>> CreateAsync(CreateLocationRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
