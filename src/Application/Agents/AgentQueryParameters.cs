namespace PIPDC.Application.Agents;

public class AgentQueryParameters
{
    public string? Keyword { get; set; }
    public bool? IsVerified { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;

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
}
