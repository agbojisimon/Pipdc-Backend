using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;

namespace PIPDC.Application.Blog;

public class TagService(IAppDbContext dbContext) : ITagService
{
    public async Task<Result<IReadOnlyList<TagDto>>> GetAllAsync(CancellationToken ct)
    {
        var tags = await dbContext.Tags
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(
                t.Id,
                t.Name,
                t.Slug,
                t.BlogPostTags.Count))
            .ToListAsync(ct);

        return Result<IReadOnlyList<TagDto>>.Success(tags);
    }

    public async Task<Result<TagDto>> CreateAsync(CreateTagRequest request, CancellationToken ct)
    {
        var slug = Slugify(request.Slug ?? request.Name);

        if (await dbContext.Tags.AnyAsync(t => t.Name == request.Name, ct))
            return Result<TagDto>.Failure(
                Error.Validation("tag.duplicate", $"A tag named '{request.Name}' already exists."));

        if (await dbContext.Tags.AnyAsync(t => t.Slug == slug, ct))
            slug = await EnsureUniqueSlugAsync(slug, ct);

        var tag = new Tag
        {
            Name = request.Name,
            Slug = slug,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync(ct);

        return Result<TagDto>.Success(new TagDto(tag.Id, tag.Name, tag.Slug, 0));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var tag = await dbContext.Tags.FindAsync([id], ct);
        if (tag is null)
            return Result.Failure(Error.NotFound("tag.notfound", $"Tag with id {id} was not found."));

        dbContext.Tags.Remove(tag);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLower(), "[^a-z0-9]+", "-").Trim('-');
        return slug.Length == 0 ? "tag" : slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        var candidate = baseSlug;
        var suffix = 2;
        while (await dbContext.Tags.AnyAsync(t => t.Slug == candidate, ct))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return candidate;
    }
}
