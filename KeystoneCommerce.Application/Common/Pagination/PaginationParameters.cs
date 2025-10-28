namespace KeystoneCommerce.Application.Common.Pagination;

public class PaginationParameters
{
    private const int MaxPageSize = 30;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    
    public string? SortBy { get; set; } = string.Empty;
    public string? SortOrder { get; set; } = string.Empty;
    
    public string? SearchBy { get; set; } = string.Empty;
    public string? SearchValue { get; set; } = string.Empty;
    
    public int TotalCount { get; set;  } = 0;
}