namespace PIPDC.Application.Properties;

public class PropertyQueryParameters
{
    public string? Keyword { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Bedrooms { get; set; }
    public string? PropertyType { get; set; }
    public string? ListingType { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;

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
        set => _pageSize = value is < 1 ? 10 : value > 50 ? 50 : value;
    }
}
