namespace PIPDC.Application.Developments;

public class DevelopmentProjectQueryParameters
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public bool? Featured { get; set; }
    public int? LocationId { get; set; }

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

public class DevelopmentTrackingQueryParameters
{
    public int? ProjectId { get; set; }
    public string? UserId { get; set; }

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
