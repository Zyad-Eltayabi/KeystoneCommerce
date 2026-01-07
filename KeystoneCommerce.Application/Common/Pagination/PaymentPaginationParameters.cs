namespace KeystoneCommerce.Application.Common.Pagination;

public class PaymentPaginationParameters : PaginationParameters
{
    public int? Status { get; set; }
    public int? Provider { get; set; }
}
