using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Blog;

public class BlogService(IAppDbContext dbContext) : IBlogService
{
    public async Task<Result<IReadOnlyList<BlogPostDto>>> GetAllAsync(BlogPostQueryParameters q, CancellationToken ct)
    {
        IQueryable<Domain.Entities.BlogPost> query = dbContext.BlogPosts;

        var hasStatus = false;
        var status = BlogPostStatus.Published;
        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<BlogPostStatus>(q.Status, true, out var parsedStatus))
        {
            hasStatus = true;
            status = parsedStatus;
        }

        if (!hasStatus)
            query = query.Where(b => b.Status == BlogPostStatus.Published);
        else
            query = query.Where(b => b.Status == status);

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(keyword)
                                  || b.Excerpt!.ToLower().Contains(keyword)
                                  || b.Content.ToLower().Contains(keyword));
        }

        var items = await query
            .OrderByDescending(b => b.PublishedAt != null ? b.PublishedAt : b.CreatedAt)
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(BlogPostMappers.ToDto).ToList();

        return Result<IReadOnlyList<BlogPostDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<BlogPostDto>>> GetAllManagedAsync(BlogPostQueryParameters q, CancellationToken ct)
    {
        IQueryable<Domain.Entities.BlogPost> query = dbContext.BlogPosts;

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(keyword)
                                  || b.Excerpt!.ToLower().Contains(keyword)
                                  || b.Content.ToLower().Contains(keyword));
        }

        var items = await query
            .OrderByDescending(b => b.PublishedAt != null ? b.PublishedAt : b.CreatedAt)
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(BlogPostMappers.ToDto).ToList();

        return Result<IReadOnlyList<BlogPostDto>>.Success(dtos);
    }

    public async Task<Result<BlogPostDto>> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var post = await dbContext.BlogPosts
            .FirstOrDefaultAsync(b => b.Slug == slug, ct);

        if (post is null)
            return Result<BlogPostDto>.Failure(
                Error.NotFound("blog.notfound", $"Blog post with slug '{slug}' was not found."));

        return Result<BlogPostDto>.Success(post.ToDto());
    }

    public async Task<Result<BlogPostDto>> CreateAsync(CreateBlogPostRequest request, CancellationToken ct)
    {
        var status = ResolveStatus(request.Status, out var parsedStatus);
        if (!status)
            return Result<BlogPostDto>.Failure(
                Error.Validation("blog.invalidstatus", $"'{request.Status}' is not a valid blog post status."));

        var slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, ct);

        var post = new Domain.Entities.BlogPost
        {
            Title = request.Title,
            Content = request.Content,
            Slug = slug,
            Excerpt = request.Excerpt,
            CoverImageUrl = request.CoverImageUrl,
            Status = parsedStatus,
            PublishedAt = parsedStatus == BlogPostStatus.Published ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.BlogPosts.Add(post);
        await dbContext.SaveChangesAsync(ct);

        return Result<BlogPostDto>.Success(post.ToDto());
    }

    public async Task<Result<BlogPostDto>> UpdateAsync(int id, UpdateBlogPostRequest request, CancellationToken ct)
    {
        var post = await dbContext.BlogPosts.FindAsync([id], ct);
        if (post is null)
            return Result<BlogPostDto>.Failure(
                Error.NotFound("blog.notfound", $"Blog post with id {id} was not found."));

        if (!Enum.TryParse<BlogPostStatus>(request.Status, true, out var status))
            return Result<BlogPostDto>.Failure(
                Error.Validation("blog.invalidstatus", $"'{request.Status}' is not a valid blog post status."));

        post.Title = request.Title;
        post.Content = request.Content;
        post.Slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, ct, excludeId: id);
        post.Excerpt = request.Excerpt;
        post.CoverImageUrl = request.CoverImageUrl;
        post.Status = status;
        if (status == BlogPostStatus.Published && post.PublishedAt is null)
            post.PublishedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result<BlogPostDto>.Success(post.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var post = await dbContext.BlogPosts.FindAsync([id], ct);
        if (post is null)
            return Result.Failure(
                Error.NotFound("blog.notfound", $"Blog post with id {id} was not found."));

        dbContext.BlogPosts.Remove(post);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static bool ResolveStatus(string? value, out BlogPostStatus status)
    {
        status = BlogPostStatus.Published;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        return Enum.TryParse(value, true, out status);
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLower(), "[^a-z0-9]+", "-").Trim('-');
        return slug.Length == 0 ? "post" : slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string? slug, string title, CancellationToken ct, int? excludeId = null)
    {
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(slug) ? title : slug);
        var candidate = baseSlug;
        var suffix = 2;

        while (await dbContext.BlogPosts.AnyAsync(b => b.Slug == candidate && (excludeId == null || b.Id != excludeId), ct))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
