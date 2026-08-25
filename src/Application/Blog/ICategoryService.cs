using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Blog;

public interface ICategoryService
{
    Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
