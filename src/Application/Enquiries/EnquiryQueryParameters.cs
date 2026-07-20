namespace PIPDC.Application.Enquiries;

public class EnquiryQueryParameters
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int? PropertyId { get; set; }
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
