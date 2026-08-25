namespace PIPDC.Application.Blog;

public class BlogPostQueryParameters
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int? CategoryId { get; set; }
    public int? TagId { get; set; }

    private int _pageNumber = 1;
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 100 ? 10 : value;
    }
}
