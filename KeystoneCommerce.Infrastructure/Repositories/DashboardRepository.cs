using KeystoneCommerce.Application.DTOs.Dashboard;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardRepository> _logger;

    public DashboardRepository(ApplicationDbContext context, ILogger<DashboardRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SalesMetricsDto> GetSalesMetricsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        try
        {
            var metrics = await _context.Orders
                .Where(o => o.CreatedAt >= startOfMonth)
                .GroupBy(_ => 1)
                .Select(g => new SalesMetricsDto
                {
                    MonthlyRevenue = g.Where(o => o.IsPaid).Sum(o => o.Total),
                    MonthlyOrdersCount = g.Count(),
                    TodayRevenue = g.Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow && o.IsPaid).Sum(o => o.Total),
                    TodayOrdersCount = g.Count(o => o.CreatedAt >= today && o.CreatedAt < tomorrow),
                    AverageOrderValue = g.Where(o => o.IsPaid).Select(o => (decimal?)o.Total).Average() ?? 0,
                    PendingOrdersCount = g.Count(o => o.Status == OrderStatus.Processing),
                    PaidOrdersCount = g.Count(o => o.Status == OrderStatus.Paid),
                    CancelledOrdersCount = g.Count(o => o.Status == OrderStatus.Cancelled),
                    FailedOrdersCount = g.Count(o => o.Status == OrderStatus.Failed)
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return metrics ?? new SalesMetricsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sales metrics");
            return new SalesMetricsDto();
        }
    }

    public async Task<InventoryMetricsDto> GetInventoryMetricsAsync()
    {
        const int criticalThreshold = 5;
        const int lowThreshold = 20;
        const int warningThreshold = 50;

        try
        {
            var totalProducts = await _context.Products.CountAsync();

            var lowStockProducts = await _context.Products
                .Where(p => p.QTY < warningThreshold)
                .OrderBy(p => p.QTY)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CurrentStock = p.QTY,
                    ImageName = p.ImageName,
                    StockLevel = p.QTY < criticalThreshold ? StockLevel.Critical :
                                p.QTY < lowThreshold ? StockLevel.Low : StockLevel.Warning
                })
                .AsNoTracking()
                .ToListAsync();

            return new InventoryMetricsDto
            {
                TotalProducts = totalProducts,
                LowStockProductsCount = lowStockProducts.Count,
                LowStockProducts = lowStockProducts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching inventory metrics");
            return new InventoryMetricsDto
            {
                TotalProducts = 0,
                LowStockProductsCount = 0,
                LowStockProducts = []
            };
        }
    }

    public async Task<List<RevenueTrendDto>> GetRevenueTrendAsync(int days)
    {
        try
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
            var endDate = DateTime.UtcNow.Date.AddDays(1);

            var trend = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new RevenueTrendDto
                {
                    Date = g.Key,
                    Revenue = g.Where(o => o.IsPaid).Sum(o => o.Total),
                    OrdersCount = g.Count()
                })
                .OrderBy(r => r.Date)
                .AsNoTracking()
                .ToListAsync();

            // Fill in missing dates with zero revenue
            var allDates = Enumerable.Range(0, days)
                .Select(i => startDate.AddDays(i))
                .ToList();

            var completeTrend = allDates
                .Select(date => trend.FirstOrDefault(t => t.Date == date) ?? new RevenueTrendDto
                {
                    Date = date,
                    Revenue = 0,
                    OrdersCount = 0
                })
                .ToList();

            return completeTrend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching revenue trend for {Days} days", days);
            // Return empty trend with dates
            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
            return Enumerable.Range(0, days)
                .Select(i => new RevenueTrendDto
                {
                    Date = startDate.AddDays(i),
                    Revenue = 0,
                    OrdersCount = 0
                })
                .ToList();
        }
    }

    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(int topCount)
    {
        try
        {
            var topProducts = await _context.OrderItems
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.Product.Title,
                    oi.Product.ImageName,
                    oi.Product.Price
                })
                .Select(g => new TopSellingProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductTitle = g.Key.Title,
                    ImageName = g.Key.ImageName,
                    Price = g.Key.Price,
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(topCount)
                .AsNoTracking()
                .ToListAsync();

            return topProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top selling products");
            return [];
        }
    }

    public async Task<List<CouponPerformanceDto>> GetTopCouponsPerformanceAsync(int topCount)
    {
        try
        {
            var coupons = await _context.Orders
                .Where(o => o.CouponId != null && o.IsPaid)
                .GroupBy(o => new
                {
                    o.CouponId,
                    o.Coupon!.Code,
                    o.Coupon.DiscountPercentage
                })
                .Select(g => new CouponPerformanceDto
                {
                    CouponId = g.Key.CouponId!.Value,
                    CouponCode = g.Key.Code,
                    DiscountPercentage = g.Key.DiscountPercentage,
                    UsageCount = g.Count(),
                    TotalDiscountGiven = g.Sum(o => o.Discount),
                    TotalRevenueWithCoupon = g.Sum(o => o.Total)
                })
                .OrderByDescending(c => c.UsageCount)
                .Take(topCount)
                .AsNoTracking()
                .ToListAsync();

            return coupons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top coupons performance");
            return [];
        }
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        try
        {
            var health = await _context.Payments
                .GroupBy(_ => 1)
                .Select(g => new SystemHealthDto
                {
                    FailedPaymentsCount = g.Count(p => p.Status == PaymentStatus.Failed),
                    PendingPaymentsCount = g.Count(p => p.Status == PaymentStatus.Processing)
                })
                .AsNoTracking()
                .FirstOrDefaultAsync() ?? new SystemHealthDto();

            var reservations = await _context.Set<InventoryReservation>()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    ActiveCount = g.Count(r => r.Status == ReservationStatus.Active),
                    ExpiredCount = g.Count(r => r.Status == ReservationStatus.Released)
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (reservations != null)
            {
                health.ActiveReservationsCount = reservations.ActiveCount;
                health.ExpiredReservationsCount = reservations.ExpiredCount;
            }

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching system health");
            return new SystemHealthDto();
        }
    }

    public async Task<OperationalAlertsDto> GetOperationalAlertsAsync()
    {
        const int criticalThreshold = 5;
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        try
        {
            var criticalStock = await _context.Products
                .Where(p => p.QTY < criticalThreshold)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CurrentStock = p.QTY,
                    ImageName = p.ImageName,
                    StockLevel = StockLevel.Critical
                })
                .OrderBy(p => p.CurrentStock)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            var failedOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Failed && o.CreatedAt >= yesterday);

            var pendingPayments = await _context.Payments
                .CountAsync(p => p.Status == PaymentStatus.Processing);

            return new OperationalAlertsDto
            {
                CriticalStockAlerts = criticalStock,
                FailedOrdersLast24Hours = failedOrders,
                PendingPaymentsCount = pendingPayments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching operational alerts");
            return new OperationalAlertsDto
            {
                CriticalStockAlerts = [],
                FailedOrdersLast24Hours = 0,
                PendingPaymentsCount = 0
            };
        }
    }

    public async Task<List<RecentActivityDto>> GetRecentOrdersAsync(int count)
    {
        try
        {
            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .Select(o => new RecentActivityDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.ShippingAddress.FullName,
                    Total = o.Total,
                    Status = o.Status.ToString(),
                    PaymentMethod = o.Payment != null ? o.Payment.Provider.ToString() : "Pending",
                    CreatedAt = o.CreatedAt
                })
                .AsNoTracking()
                .ToListAsync();
            return recentOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent orders");
            return [];
        }
    }

    public async Task<Dictionary<string, int>> GetOrderStatusDistributionAsync()
    {
        try
        {
            var distribution = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order status distribution");
            return [];
        }
    }
}
