using System.ComponentModel.DataAnnotations;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Blog;

public record BlogPostDto(
    int Id,
    string Title,
    string Slug,
    string Content,
    string? Excerpt,
    string? CoverImageUrl,
    string Status,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int ReadMinutes);

public record CreateBlogPostRequest(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(100000)] string Content,
    [MaxLength(200)] string? Slug,
    [MaxLength(1000)] string? Excerpt,
    [MaxLength(500)] string? CoverImageUrl,
    string? Status);

public record UpdateBlogPostRequest(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(100000)] string Content,
    [MaxLength(200)] string? Slug,
    [MaxLength(1000)] string? Excerpt,
    [MaxLength(500)] string? CoverImageUrl,
    [Required] string Status);

public static class BlogPostMappers
{
    public static BlogPostDto ToDto(this Domain.Entities.BlogPost post) =>
        new(
            post.Id,
            post.Title,
            post.Slug,
            post.Content,
            post.Excerpt,
            post.CoverImageUrl,
            post.Status.ToString(),
            post.PublishedAt,
            post.CreatedAt,
            post.UpdatedAt,
            Math.Max(1, (int)Math.Ceiling(post.Content.Length / 400.0)));

    public static string ToFrontendStatus(BlogPostStatus status) =>
        status == BlogPostStatus.Published ? "Published" : status.ToString();
}
