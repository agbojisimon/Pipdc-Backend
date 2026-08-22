using PIPDC.Domain.Common;

namespace PIPDC.Application.Developments;

public interface IDevelopmentUpdateService
{
    Task<Result<IReadOnlyList<DevelopmentUpdateDto>>> GetByProjectAsync(int projectId, CancellationToken ct);
    Task<Result<DevelopmentUpdateDto>> CreateAsync(int projectId, CreateDevelopmentUpdateRequest request, CancellationToken ct);
    Task<Result<DevelopmentUpdateDto>> UpdateAsync(int projectId, int updateId, UpdateDevelopmentUpdateRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int projectId, int updateId, CancellationToken ct);
}
