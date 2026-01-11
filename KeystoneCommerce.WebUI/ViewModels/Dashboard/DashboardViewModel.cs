namespace KeystoneCommerce.WebUI.ViewModels.Dashboard;

public class DashboardViewModel
{
    public SalesMetricsViewModel SalesMetrics { get; set; } = null!;
    public InventoryMetricsViewModel InventoryMetrics { get; set; } = null!;
    public List<RevenueTrendViewModel> Last7DaysTrend { get; set; } = [];
    public List<RevenueTrendViewModel> Last30DaysTrend { get; set; } = [];
    public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = [];
    public List<CouponPerformanceViewModel> TopCoupons { get; set; } = [];
    public SystemHealthViewModel SystemHealth { get; set; } = null!;
    public OperationalAlertsViewModel OperationalAlerts { get; set; } = null!;
    public List<RecentOrderViewModel> RecentOrders { get; set; } = [];
    public Dictionary<string, int> OrderStatusDistribution { get; set; } = new();
}
