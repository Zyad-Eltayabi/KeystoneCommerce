namespace KeystoneCommerce.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public SalesMetricsDto SalesMetrics { get; set; } = null!;
    public InventoryMetricsDto InventoryMetrics { get; set; } = null!;
    public List<RevenueTrendDto> Last7DaysTrend { get; set; } = [];
    public List<RevenueTrendDto> Last30DaysTrend { get; set; } = [];
    public List<TopSellingProductDto> TopSellingProducts { get; set; } = [];
    public List<CouponPerformanceDto> TopCoupons { get; set; } = [];
    public SystemHealthDto SystemHealth { get; set; } = null!;
    public OperationalAlertsDto OperationalAlerts { get; set; } = null!;
    public List<RecentActivityDto> RecentOrders { get; set; } = [];
    public Dictionary<string, int> OrderStatusDistribution { get; set; } = new();
}
