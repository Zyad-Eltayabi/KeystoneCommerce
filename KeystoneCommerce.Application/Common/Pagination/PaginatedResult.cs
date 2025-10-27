namespace KeystoneCommerce.Application.Common.Pagination;

public class PaginatedResult<T> where T : class
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? SortBy { get; set; } = string.Empty;
    public string? SortOrder { get; set; } = string.Empty;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}