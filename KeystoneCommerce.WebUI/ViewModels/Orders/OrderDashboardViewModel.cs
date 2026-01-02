using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.WebUI.ViewModels.Orders;

public class OrderDashboardViewModel
{
    public OrderPaginatedResult<OrderDto> PaginatedOrders { get; set; } = null!;
    public OrderAnalyticsViewModel MonthlyAnalytics { get; set; } = null!;
    public OrderAnalyticsViewModel TodayAnalytics { get; set; } = null!;
}
