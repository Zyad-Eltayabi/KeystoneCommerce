namespace KeystoneCommerce.Application.Common.Pagination;

public class PaymentPaginatedResult<T> : PaginatedResult<T> where T : class
{
    public int? Status { get; set; }
    public int? Provider { get; set; }
}
