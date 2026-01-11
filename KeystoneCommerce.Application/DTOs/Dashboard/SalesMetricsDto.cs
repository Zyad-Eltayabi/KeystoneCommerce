namespace KeystoneCommerce.Application.DTOs.Dashboard;

public class SalesMetricsDto
{
    public decimal TodayRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TodayOrdersCount { get; set; }
    public int MonthlyOrdersCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int PendingOrdersCount { get; set; }
    public int PaidOrdersCount { get; set; }
    public int CancelledOrdersCount { get; set; }
    public int FailedOrdersCount { get; set; }
}
