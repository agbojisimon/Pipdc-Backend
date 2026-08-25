using System.ComponentModel.DataAnnotations;

namespace PIPDC.Application.Blog;

public record CategoryDto(
    int Id,
    string Name,
    string Slug,
    int BlogPostCount);

public record CreateCategoryRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(100)] string? Slug);
