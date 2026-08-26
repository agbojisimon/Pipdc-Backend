using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Blog;

public class BlogService(IAppDbContext dbContext) : IBlogService
{
    public async Task<Result<PaginatedResult<BlogPostDto>>> GetAllAsync(BlogPostQueryParameters q, CancellationToken ct)
    {
        IQueryable<BlogPost> query = dbContext.BlogPosts;

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

        if (q.CategoryId.HasValue)
            query = query.Where(b => b.CategoryId == q.CategoryId);

        if (q.TagId.HasValue)
            query = query.Where(b => b.BlogPostTags.Any(bpt => bpt.TagId == q.TagId));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(b => b.Category)
            .Include(b => b.BlogPostTags).ThenInclude(bpt => bpt.Tag)
            .OrderByDescending(b => b.PublishedAt != null ? b.PublishedAt : b.CreatedAt)
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(BuildDto).ToList();

        return Result<PaginatedResult<BlogPostDto>>.Success(
            PaginatedResult<BlogPostDto>.Create(dtos, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result<PaginatedResult<BlogPostDto>>> GetAllManagedAsync(BlogPostQueryParameters q, CancellationToken ct)
    {
        IQueryable<BlogPost> query = dbContext.BlogPosts;

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(keyword)
                                  || b.Excerpt!.ToLower().Contains(keyword)
                                  || b.Content.ToLower().Contains(keyword));
        }

        if (q.CategoryId.HasValue)
            query = query.Where(b => b.CategoryId == q.CategoryId);

        if (q.TagId.HasValue)
            query = query.Where(b => b.BlogPostTags.Any(bpt => bpt.TagId == q.TagId));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(b => b.Category)
            .Include(b => b.BlogPostTags).ThenInclude(bpt => bpt.Tag)
            .OrderByDescending(b => b.PublishedAt != null ? b.PublishedAt : b.CreatedAt)
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(BuildDto).ToList();

        return Result<PaginatedResult<BlogPostDto>>.Success(
            PaginatedResult<BlogPostDto>.Create(dtos, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result<BlogPostDto>> GetBySlugAsync(string slug, bool isAdmin, CancellationToken ct)
    {
        var query = dbContext.BlogPosts
            .Include(b => b.Category)
            .Include(b => b.BlogPostTags).ThenInclude(bpt => bpt.Tag)
            .Where(b => b.Slug == slug);

        if (!isAdmin)
            query = query.Where(b => b.Status == BlogPostStatus.Published);

        var post = await query.FirstOrDefaultAsync(ct);

        if (post is null)
            return Result<BlogPostDto>.Failure(
                Error.NotFound("blog.notfound", $"Blog post with slug '{slug}' was not found."));

        return Result<BlogPostDto>.Success(BuildDto(post));
    }

    public async Task<Result<IReadOnlyList<BlogPostDto>>> GetRelatedAsync(string slug, CancellationToken ct)
    {
        var post = await dbContext.BlogPosts
            .Include(b => b.BlogPostTags)
            .FirstOrDefaultAsync(b => b.Slug == slug, ct);

        if (post is null)
            return Result<IReadOnlyList<BlogPostDto>>.Failure(
                Error.NotFound("blog.notfound", $"Blog post with slug '{slug}' was not found."));

        var tagIds = post.BlogPostTags.Select(bpt => bpt.TagId).ToList();

        var related = await dbContext.BlogPosts
            .Include(b => b.Category)
            .Include(b => b.BlogPostTags).ThenInclude(bpt => bpt.Tag)
            .Where(b => b.Status == BlogPostStatus.Published && b.Id != post.Id)
            .OrderByDescending(b => b.BlogPostTags.Count(bpt => tagIds.Contains(bpt.TagId)))
            .ThenByDescending(b => b.PublishedAt)
            .Take(3)
            .ToListAsync(ct);

        return Result<IReadOnlyList<BlogPostDto>>.Success(
            related.Select(BuildDto).ToList());
    }

    public async Task<Result<BlogPostDto>> CreateAsync(CreateBlogPostRequest request, string? authorUserId, CancellationToken ct)
    {
        var status = ResolveStatus(request.Status, out var parsedStatus);
        if (!status)
            return Result<BlogPostDto>.Failure(
                Error.Validation("blog.invalidstatus", $"'{request.Status}' is not a valid blog post status."));

        var slug = await EnsureUniqueSlugAsync(request.Slug, request.Title, ct);

        var post = new BlogPost
        {
            Title = request.Title,
            Content = request.Content,
            Slug = slug,
            Excerpt = request.Excerpt,
            CoverImageUrl = request.CoverImageUrl,
            CoverImagePublicId = request.CoverImagePublicId,
            Status = parsedStatus,
            PublishedAt = parsedStatus == BlogPostStatus.Published ? DateTime.UtcNow : null,
            KeyQuote = request.KeyQuote,
            CategoryId = request.CategoryId,
            AuthorUserId = authorUserId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.BlogPosts.Add(post);

        if (request.TagIds is { Count: > 0 })
        {
            foreach (var tagId in request.TagIds)
                post.BlogPostTags.Add(new BlogPostTag { BlogPost = post, TagId = tagId });
        }

        await dbContext.SaveChangesAsync(ct);

        var saved = await dbContext.BlogPosts
            .Include(b => b.Category)
            .Include(b => b.BlogPostTags).ThenInclude(bpt => bpt.Tag)
            .FirstAsync(b => b.Id == post.Id, ct);
        return Result<BlogPostDto>.Success(BuildDto(saved));
    }

    public async Task<Result<BlogPostDto>> UpdateAsync(int id, UpdateBlogPostRequest request, CancellationToken ct)
    {
        var post = await dbContext.BlogPosts
            .Include(b => b.BlogPostTags)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

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
        post.CoverImagePublicId = request.CoverImagePublicId;
        post.Status = status;
        post.KeyQuote = request.KeyQuote;
        post.CategoryId = request.CategoryId;

        if (status == BlogPostStatus.Published && post.PublishedAt is null)
            post.PublishedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        if (request.TagIds is not null)
        {
            dbContext.BlogPostTags.RemoveRange(post.BlogPostTags);
            post.BlogPostTags.Clear();
            foreach (var tagId in request.TagIds)
                post.BlogPostTags.Add(new BlogPostTag { BlogPostId = id, TagId = tagId });
        }

        await dbContext.SaveChangesAsync(ct);

        var saved = await dbContext.BlogPosts
            .Include(b => b.Category)
            .Include(b => b.BlogPostTags).ThenInclude(bpt => bpt.Tag)
            .FirstAsync(b => b.Id == id, ct);
        return Result<BlogPostDto>.Success(BuildDto(saved));
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

    public async Task<Result<BlogPostDto>> PublishAsync(int id, CancellationToken ct)
    {
        var post = await dbContext.BlogPosts.FindAsync([id], ct);
        if (post is null)
            return Result<BlogPostDto>.Failure(
                Error.NotFound("blog.notfound", $"Blog post with id {id} was not found."));

        post.Status = BlogPostStatus.Published;
        post.PublishedAt ??= DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        var saved = await dbContext.BlogPosts
            .Include(b => b.Category)
            .Include(b => b.BlogPostTags).ThenInclude(bpt => bpt.Tag)
            .FirstAsync(b => b.Id == id, ct);
        return Result<BlogPostDto>.Success(BuildDto(saved));
    }

    public async Task<Result<BlogPostDto>> UnpublishAsync(int id, CancellationToken ct)
    {
        var post = await dbContext.BlogPosts.FindAsync([id], ct);
        if (post is null)
            return Result<BlogPostDto>.Failure(
                Error.NotFound("blog.notfound", $"Blog post with id {id} was not found."));

        post.Status = BlogPostStatus.Draft;
        post.PublishedAt = null;
        post.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        var saved = await dbContext.BlogPosts
            .Include(b => b.Category)
            .Include(b => b.BlogPostTags).ThenInclude(bpt => bpt.Tag)
            .FirstAsync(b => b.Id == id, ct);
        return Result<BlogPostDto>.Success(BuildDto(saved));
    }

    private static BlogPostDto BuildDto(BlogPost post) => post.ToDto();

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
