using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Blog;

public interface ITagService
{
    Task<Result<IReadOnlyList<TagDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<TagDto>> CreateAsync(CreateTagRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
