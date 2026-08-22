using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Developments;

public interface IDevelopmentProjectService
{
    Task<Result<PaginatedResult<DevelopmentProjectDto>>> GetAllAsync(DevelopmentProjectQueryParameters q, CancellationToken ct);
    Task<Result<DevelopmentProjectDetailDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<DevelopmentProjectDto>> CreateAsync(CreateDevelopmentProjectRequest request, CancellationToken ct);
    Task<Result<DevelopmentProjectDto>> UpdateAsync(int id, UpdateDevelopmentProjectRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result> UpdateFeaturedAsync(int id, bool featured, CancellationToken ct);
}
