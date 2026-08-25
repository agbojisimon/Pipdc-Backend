using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<BlogPostTag> BlogPostTags { get; set; } = [];
}
