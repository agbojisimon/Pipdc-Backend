using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Blog;

public interface IBlogService
{
    Task<Result<IReadOnlyList<BlogPostDto>>> GetAllAsync(BlogPostQueryParameters q, CancellationToken ct);
    Task<Result<IReadOnlyList<BlogPostDto>>> GetAllManagedAsync(BlogPostQueryParameters q, CancellationToken ct);
    Task<Result<BlogPostDto>> GetBySlugAsync(string slug, CancellationToken ct);
    Task<Result<BlogPostDto>> CreateAsync(CreateBlogPostRequest request, CancellationToken ct);
    Task<Result<BlogPostDto>> UpdateAsync(int id, UpdateBlogPostRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
