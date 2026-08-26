using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Blog;

public interface IBlogService
{
    Task<Result<PaginatedResult<BlogPostDto>>> GetAllAsync(BlogPostQueryParameters q, CancellationToken ct);
    Task<Result<PaginatedResult<BlogPostDto>>> GetAllManagedAsync(BlogPostQueryParameters q, CancellationToken ct);
    Task<Result<BlogPostDto>> GetBySlugAsync(string slug, bool isAdmin, CancellationToken ct);
    Task<Result<IReadOnlyList<BlogPostDto>>> GetRelatedAsync(string slug, CancellationToken ct);
    Task<Result<BlogPostDto>> CreateAsync(CreateBlogPostRequest request, string? authorUserId, CancellationToken ct);
    Task<Result<BlogPostDto>> UpdateAsync(int id, UpdateBlogPostRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<BlogPostDto>> PublishAsync(int id, CancellationToken ct);
    Task<Result<BlogPostDto>> UnpublishAsync(int id, CancellationToken ct);
}
