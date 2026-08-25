using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;

namespace PIPDC.Application.Blog;

public class CategoryService(IAppDbContext dbContext) : ICategoryService
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken ct)
    {
        var categories = await dbContext.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Slug,
                c.BlogPosts.Count))
            .ToListAsync(ct);

        return Result<IReadOnlyList<CategoryDto>>.Success(categories);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        var slug = Slugify(request.Slug ?? request.Name);

        if (await dbContext.Categories.AnyAsync(c => c.Name == request.Name, ct))
            return Result<CategoryDto>.Failure(
                Error.Validation("category.duplicate", $"A category named '{request.Name}' already exists."));

        if (await dbContext.Categories.AnyAsync(c => c.Slug == slug, ct))
            slug = await EnsureUniqueSlugAsync(slug, ct);

        var category = new Category
        {
            Name = request.Name,
            Slug = slug,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(ct);

        return Result<CategoryDto>.Success(new CategoryDto(category.Id, category.Name, category.Slug, 0));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await dbContext.Categories.FindAsync([id], ct);
        if (category is null)
            return Result.Failure(Error.NotFound("category.notfound", $"Category with id {id} was not found."));

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLower(), "[^a-z0-9]+", "-").Trim('-');
        return slug.Length == 0 ? "category" : slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        var candidate = baseSlug;
        var suffix = 2;
        while (await dbContext.Categories.AnyAsync(c => c.Slug == candidate, ct))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return candidate;
    }
}
