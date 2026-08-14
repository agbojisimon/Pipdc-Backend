namespace PIPDC.Application.Properties;

public class PropertyQueryParameters
{
    public string? Query { get; set; }
    public string? Keyword { get; set; }
    public string? Location { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Type { get; set; }
    public string? PropertyType { get; set; }
    public string? ListingType { get; set; }
    public string? Status { get; set; }
    public int? AgentId { get; set; }
    public string? Sort { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }

    public int? Page { get; set; }

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

    public int EffectivePageNumber => Math.Max(Page ?? PageNumber, 1);
    public int EffectivePageSize => PageSize;
}
