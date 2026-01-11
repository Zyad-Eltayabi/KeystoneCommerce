namespace KeystoneCommerce.WebUI.ViewModels.Dashboard;

public class SalesMetricsViewModel
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
