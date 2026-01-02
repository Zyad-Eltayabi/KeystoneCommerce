namespace KeystoneCommerce.Application.DTOs.Order;

public class OrderDashboardDto
{
    public OrderPaginatedResult<OrderDto> PaginatedOrders { get; set; } = null!;
    public OrderAnalyticsDto MonthlyAnalytics { get; set; } = null!;
    public OrderAnalyticsDto TodayAnalytics { get; set; } = null!;
}
