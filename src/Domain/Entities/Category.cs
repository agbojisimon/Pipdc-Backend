using PIPDC.Domain.Common;

namespace PIPDC.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<BlogPost> BlogPosts { get; set; } = [];
}
