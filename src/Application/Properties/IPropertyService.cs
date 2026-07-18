using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Properties;

public interface IPropertyService
{
    Task<Result<PaginatedResult<PropertyDto>>> GetAllAsync(PropertyQueryParameters queryParams, CancellationToken ct);
    Task<Result<PropertyDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PropertyDto>> CreateAsync(CreatePropertyRequest request, CancellationToken ct);
    Task<Result<PropertyDto>> UpdateAsync(int id, UpdatePropertyRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
