using System.ComponentModel.DataAnnotations;

namespace PIPDC.Application.Blog;

public record TagDto(
    int Id,
    string Name,
    string Slug,
    int BlogPostCount);

public record CreateTagRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(100)] string? Slug);
