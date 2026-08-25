using PIPDC.Domain.Common;
using PIPDC.Domain.Enums;

namespace PIPDC.Domain.Entities;

public class BlogPost : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CoverImagePublicId { get; set; }
    public BlogPostStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? KeyQuote { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string? AuthorUserId { get; set; }
    public ICollection<BlogPostTag> BlogPostTags { get; set; } = [];
}
