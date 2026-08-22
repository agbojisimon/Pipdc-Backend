using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Developments;

public interface IDevelopmentTrackingService
{
    Task<Result<PaginatedResult<DevelopmentTrackingDto>>> GetTrackedAsync(string userId, DevelopmentProjectQueryParameters q, CancellationToken ct);
    Task<Result> TrackAsync(string userId, int projectId, int? unitId, CancellationToken ct);
    Task<Result> StopTrackingAsync(string userId, int projectId, CancellationToken ct);
    Task<bool> IsTrackingAsync(string userId, int projectId, CancellationToken ct);
    Task<Result<PaginatedResult<AdminDevelopmentTrackingDto>>> AdminGetAllAsync(DevelopmentTrackingQueryParameters q, CancellationToken ct);
    Task<Result<IReadOnlyList<AdminDevelopmentTrackingDto>>> AdminGetByProjectAsync(int projectId, CancellationToken ct);
    Task<Result<IReadOnlyList<AdminDevelopmentTrackingDto>>> AdminGetByUserAsync(string userId, CancellationToken ct);
    Task<Result> AdminRemoveTrackingAsync(int trackingId, CancellationToken ct);
    Task<Result> AdminUpdateTrackingStatusAsync(int trackingId, string status, CancellationToken ct);
}
