using KeystoneCommerce.Application.DTOs.Dashboard;

namespace KeystoneCommerce.Application.Interfaces.Repositories;

public interface IDashboardRepository
{
    Task<SalesMetricsDto> GetSalesMetricsAsync();
    Task<InventoryMetricsDto> GetInventoryMetricsAsync();
    Task<List<RevenueTrendDto>> GetRevenueTrendAsync(int days);
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(int topCount);
    Task<List<CouponPerformanceDto>> GetTopCouponsPerformanceAsync(int topCount);
    Task<SystemHealthDto> GetSystemHealthAsync();
    Task<OperationalAlertsDto> GetOperationalAlertsAsync();
    Task<List<RecentActivityDto>> GetRecentOrdersAsync(int count);
    Task<Dictionary<string, int>> GetOrderStatusDistributionAsync();
}
