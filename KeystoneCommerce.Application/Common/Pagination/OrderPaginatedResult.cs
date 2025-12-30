namespace KeystoneCommerce.Application.Common.Pagination;

public class OrderPaginatedResult<T> : PaginatedResult<T> where T : class
{
    public int? Status { get; set; }
}
