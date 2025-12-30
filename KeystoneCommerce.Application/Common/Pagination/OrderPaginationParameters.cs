using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Application.Common.Pagination;

public class OrderPaginationParameters : PaginationParameters
{
    public int? Status { get; set; }
}
