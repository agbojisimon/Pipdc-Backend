using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Developments;

public interface IDevelopmentProjectPublicService
{
    Task<Result<PaginatedResult<DevelopmentProjectDto>>> GetPublicAllAsync(DevelopmentProjectQueryParameters q, CancellationToken ct);
    Task<Result<DevelopmentProjectDetailDto>> GetPublicBySlugAsync(string slug, CancellationToken ct);
    Task<Result<DevelopmentProjectDetailDto>> GetPublicByIdAsync(int id, CancellationToken ct);
}
