namespace PIPDC.Domain.Entities;

public class BlogPostTag
{
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
