using KeystoneCommerce.Application.DTOs.Dashboard;

namespace KeystoneCommerce.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        ILogger<DashboardService> logger)
    {
        _dashboardRepository = dashboardRepository;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        _logger.LogInformation("Fetching dashboard summary data");

        try
        {
            // Execute queries sequentially to avoid DbContext concurrency issues
            var salesMetrics = await _dashboardRepository.GetSalesMetricsAsync();
            var inventoryMetrics = await _dashboardRepository.GetInventoryMetricsAsync();
            var last7DaysTrend = await _dashboardRepository.GetRevenueTrendAsync(7);
            var last30DaysTrend = await _dashboardRepository.GetRevenueTrendAsync(30);
            var topSellingProducts = await _dashboardRepository.GetTopSellingProductsAsync(10);
            var topCoupons = await _dashboardRepository.GetTopCouponsPerformanceAsync(5);
            var systemHealth = await _dashboardRepository.GetSystemHealthAsync();
            var operationalAlerts = await _dashboardRepository.GetOperationalAlertsAsync();
            var recentOrders = await _dashboardRepository.GetRecentOrdersAsync(10);
            var orderStatusDistribution = await _dashboardRepository.GetOrderStatusDistributionAsync();

            var dashboard = new DashboardSummaryDto
            {
                SalesMetrics = salesMetrics,
                InventoryMetrics = inventoryMetrics,
                Last7DaysTrend = last7DaysTrend,
                Last30DaysTrend = last30DaysTrend,
                TopSellingProducts = topSellingProducts,
                TopCoupons = topCoupons,
                SystemHealth = systemHealth,
                OperationalAlerts = operationalAlerts,
                RecentOrders = recentOrders,
                OrderStatusDistribution = orderStatusDistribution
            };

            _logger.LogInformation("Dashboard summary data fetched successfully");

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard summary data");
            throw;
        }
    }
}
