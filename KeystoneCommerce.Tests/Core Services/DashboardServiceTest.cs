using KeystoneCommerce.Application.DTOs.Dashboard;
using Xunit;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("DashboardServiceTests")]
public class DashboardServiceTest
{
    private readonly Mock<IDashboardRepository> _mockDashboardRepository;
    private readonly Mock<ILogger<DashboardService>> _mockLogger;
    private readonly DashboardService _sut;

    public DashboardServiceTest()
    {
        _mockDashboardRepository = new Mock<IDashboardRepository>();
        _mockLogger = new Mock<ILogger<DashboardService>>();

        _sut = new DashboardService(
            _mockDashboardRepository.Object,
            _mockLogger.Object);
    }

    #region GetDashboardSummaryAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnCompleteDashboard_WhenAllDataIsAvailable()
    {
        // Arrange
        var salesMetrics = CreateSalesMetrics();
        var inventoryMetrics = CreateInventoryMetrics();
        var last7DaysTrend = CreateRevenueTrend(7);
        var last30DaysTrend = CreateRevenueTrend(30);
        var topSellingProducts = CreateTopSellingProducts(10);
        var topCoupons = CreateCouponPerformance(5);
        var systemHealth = CreateSystemHealth();
        var operationalAlerts = CreateOperationalAlerts();
        var recentOrders = CreateRecentOrders(10);
        var orderStatusDistribution = CreateOrderStatusDistribution();

        SetupAllRepositoryMocks(salesMetrics, inventoryMetrics, last7DaysTrend, last30DaysTrend,
            topSellingProducts, topCoupons, systemHealth, operationalAlerts, recentOrders, orderStatusDistribution);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.SalesMetrics.Should().BeEquivalentTo(salesMetrics);
        result.InventoryMetrics.Should().BeEquivalentTo(inventoryMetrics);
        result.Last7DaysTrend.Should().HaveCount(7);
        result.Last30DaysTrend.Should().HaveCount(30);
        result.TopSellingProducts.Should().HaveCount(10);
        result.TopCoupons.Should().HaveCount(5);
        result.SystemHealth.Should().BeEquivalentTo(systemHealth);
        result.OperationalAlerts.Should().BeEquivalentTo(operationalAlerts);
        result.RecentOrders.Should().HaveCount(10);
        result.OrderStatusDistribution.Should().HaveCount(4);

        VerifyAllRepositoryMethodsCalled();
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnDashboard_WithEmptyCollections()
    {
        // Arrange
        var salesMetrics = new SalesMetricsDto();
        var inventoryMetrics = new InventoryMetricsDto { LowStockProducts = [] };
        var last7DaysTrend = new List<RevenueTrendDto>();
        var last30DaysTrend = new List<RevenueTrendDto>();
        var topSellingProducts = new List<TopSellingProductDto>();
        var topCoupons = new List<CouponPerformanceDto>();
        var systemHealth = new SystemHealthDto();
        var operationalAlerts = new OperationalAlertsDto { CriticalStockAlerts = [] };
        var recentOrders = new List<RecentActivityDto>();
        var orderStatusDistribution = new Dictionary<string, int>();

        SetupAllRepositoryMocks(salesMetrics, inventoryMetrics, last7DaysTrend, last30DaysTrend,
            topSellingProducts, topCoupons, systemHealth, operationalAlerts, recentOrders, orderStatusDistribution);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.Should().NotBeNull();
        result.TopSellingProducts.Should().BeEmpty();
        result.TopCoupons.Should().BeEmpty();
        result.RecentOrders.Should().BeEmpty();
        result.Last7DaysTrend.Should().BeEmpty();
        result.Last30DaysTrend.Should().BeEmpty();
        result.OrderStatusDistribution.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldExecuteQueriesSequentially()
    {
        // Arrange
        var executionOrder = new List<string>();

        _mockDashboardRepository.Setup(r => r.GetSalesMetricsAsync())
            .Callback(() => executionOrder.Add("SalesMetrics"))
            .ReturnsAsync(new SalesMetricsDto());

        _mockDashboardRepository.Setup(r => r.GetInventoryMetricsAsync())
            .Callback(() => executionOrder.Add("InventoryMetrics"))
            .ReturnsAsync(new InventoryMetricsDto { LowStockProducts = [] });

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(7))
            .Callback(() => executionOrder.Add("Last7DaysTrend"))
            .ReturnsAsync(new List<RevenueTrendDto>());

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(30))
            .Callback(() => executionOrder.Add("Last30DaysTrend"))
            .ReturnsAsync(new List<RevenueTrendDto>());

        _mockDashboardRepository.Setup(r => r.GetTopSellingProductsAsync(10))
            .Callback(() => executionOrder.Add("TopSellingProducts"))
            .ReturnsAsync(new List<TopSellingProductDto>());

        _mockDashboardRepository.Setup(r => r.GetTopCouponsPerformanceAsync(5))
            .Callback(() => executionOrder.Add("TopCoupons"))
            .ReturnsAsync(new List<CouponPerformanceDto>());

        _mockDashboardRepository.Setup(r => r.GetSystemHealthAsync())
            .Callback(() => executionOrder.Add("SystemHealth"))
            .ReturnsAsync(new SystemHealthDto());

        _mockDashboardRepository.Setup(r => r.GetOperationalAlertsAsync())
            .Callback(() => executionOrder.Add("OperationalAlerts"))
            .ReturnsAsync(new OperationalAlertsDto { CriticalStockAlerts = [] });

        _mockDashboardRepository.Setup(r => r.GetRecentOrdersAsync(10))
            .Callback(() => executionOrder.Add("RecentOrders"))
            .ReturnsAsync(new List<RecentActivityDto>());

        _mockDashboardRepository.Setup(r => r.GetOrderStatusDistributionAsync())
            .Callback(() => executionOrder.Add("OrderStatusDistribution"))
            .ReturnsAsync(new Dictionary<string, int>());

        // Act
        await _sut.GetDashboardSummaryAsync();

        // Assert
        executionOrder.Should().Equal(
            "SalesMetrics",
            "InventoryMetrics",
            "Last7DaysTrend",
            "Last30DaysTrend",
            "TopSellingProducts",
            "TopCoupons",
            "SystemHealth",
            "OperationalAlerts",
            "RecentOrders",
            "OrderStatusDistribution"
        );
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldPassCorrectParameters_ToRepositoryMethods()
    {
        // Arrange
        SetupAllRepositoryMocks(
            new SalesMetricsDto(),
            new InventoryMetricsDto { LowStockProducts = [] },
            new List<RevenueTrendDto>(),
            new List<RevenueTrendDto>(),
            new List<TopSellingProductDto>(),
            new List<CouponPerformanceDto>(),
            new SystemHealthDto(),
            new OperationalAlertsDto { CriticalStockAlerts = [] },
            new List<RecentActivityDto>(),
            new Dictionary<string, int>()
        );

        // Act
        await _sut.GetDashboardSummaryAsync();

        // Assert
        _mockDashboardRepository.Verify(r => r.GetRevenueTrendAsync(7), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRevenueTrendAsync(30), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetTopSellingProductsAsync(10), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetTopCouponsPerformanceAsync(5), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRecentOrdersAsync(10), Times.Once);
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldPreserveAllSalesMetricsData()
    {
        // Arrange
        var salesMetrics = new SalesMetricsDto
        {
            TodayRevenue = 1500.50m,
            MonthlyRevenue = 45000.75m,
            TodayOrdersCount = 25,
            MonthlyOrdersCount = 350,
            AverageOrderValue = 128.57m,
            PendingOrdersCount = 12,
            PaidOrdersCount = 300,
            CancelledOrdersCount = 20,
            FailedOrdersCount = 18
        };

        SetupMinimalRepositoryMocks(salesMetrics);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.SalesMetrics.TodayRevenue.Should().Be(1500.50m);
        result.SalesMetrics.MonthlyRevenue.Should().Be(45000.75m);
        result.SalesMetrics.TodayOrdersCount.Should().Be(25);
        result.SalesMetrics.MonthlyOrdersCount.Should().Be(350);
        result.SalesMetrics.AverageOrderValue.Should().Be(128.57m);
        result.SalesMetrics.PendingOrdersCount.Should().Be(12);
        result.SalesMetrics.PaidOrdersCount.Should().Be(300);
        result.SalesMetrics.CancelledOrdersCount.Should().Be(20);
        result.SalesMetrics.FailedOrdersCount.Should().Be(18);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldPreserveAllInventoryMetricsData()
    {
        // Arrange
        var inventoryMetrics = new InventoryMetricsDto
        {
            TotalProducts = 500,
            LowStockProductsCount = 25,
            LowStockProducts = new List<LowStockProductDto>
            {
                new() { Id = 1, Title = "Product 1", CurrentStock = 3, ImageName = "img1.jpg", StockLevel = StockLevel.Critical },
                new() { Id = 2, Title = "Product 2", CurrentStock = 15, ImageName = "img2.jpg", StockLevel = StockLevel.Low }
            }
        };

        SetupMinimalRepositoryMocks(new SalesMetricsDto(), inventoryMetrics);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.InventoryMetrics.TotalProducts.Should().Be(500);
        result.InventoryMetrics.LowStockProductsCount.Should().Be(25);
        result.InventoryMetrics.LowStockProducts.Should().HaveCount(2);
        result.InventoryMetrics.LowStockProducts[0].CurrentStock.Should().Be(3);
        result.InventoryMetrics.LowStockProducts[0].StockLevel.Should().Be(StockLevel.Critical);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldPreserveRevenueTrendData()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var last7DaysTrend = new List<RevenueTrendDto>
        {
            new() { Date = today.AddDays(-6), Revenue = 100.50m, OrdersCount = 5 },
            new() { Date = today.AddDays(-5), Revenue = 200.75m, OrdersCount = 8 },
            new() { Date = today.AddDays(-4), Revenue = 150.25m, OrdersCount = 6 },
            new() { Date = today.AddDays(-3), Revenue = 300.00m, OrdersCount = 10 },
            new() { Date = today.AddDays(-2), Revenue = 250.50m, OrdersCount = 9 },
            new() { Date = today.AddDays(-1), Revenue = 180.75m, OrdersCount = 7 },
            new() { Date = today, Revenue = 220.00m, OrdersCount = 8 }
        };

        SetupMinimalRepositoryMocks(new SalesMetricsDto(), new InventoryMetricsDto { LowStockProducts = [] }, last7DaysTrend);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.Last7DaysTrend.Should().HaveCount(7);
        result.Last7DaysTrend[0].Date.Should().Be(today.AddDays(-6));
        result.Last7DaysTrend[0].Revenue.Should().Be(100.50m);
        result.Last7DaysTrend[6].Revenue.Should().Be(220.00m);
        result.Last7DaysTrend.Sum(t => t.OrdersCount).Should().Be(53);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldPreserveTopSellingProductsData()
    {
        // Arrange
        var topProducts = new List<TopSellingProductDto>
        {
            new() 
            { 
                ProductId = 1, 
                ProductTitle = "Laptop", 
                ImageName = "laptop.jpg", 
                Price = 999.99m,
                TotalQuantitySold = 150,
                TotalRevenue = 149998.50m
            },
            new() 
            { 
                ProductId = 2, 
                ProductTitle = "Mouse", 
                ImageName = "mouse.jpg", 
                Price = 29.99m,
                TotalQuantitySold = 500,
                TotalRevenue = 14995.00m
            }
        };

        SetupMinimalRepositoryMocks(new SalesMetricsDto(), new InventoryMetricsDto { LowStockProducts = [] }, 
            new List<RevenueTrendDto>(), new List<RevenueTrendDto>(), topProducts);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.TopSellingProducts.Should().HaveCount(2);
        result.TopSellingProducts[0].ProductTitle.Should().Be("Laptop");
        result.TopSellingProducts[0].TotalQuantitySold.Should().Be(150);
        result.TopSellingProducts[1].TotalRevenue.Should().Be(14995.00m);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldPreserveOrderStatusDistribution()
    {
        // Arrange
        var distribution = new Dictionary<string, int>
        {
            { "Processing", 50 },
            { "Paid", 300 },
            { "Cancelled", 20 },
            { "Failed", 10 }
        };

        SetupMinimalRepositoryMocks(
            new SalesMetricsDto(),
            new InventoryMetricsDto { LowStockProducts = [] },
            new List<RevenueTrendDto>(),
            new List<RevenueTrendDto>(),
            new List<TopSellingProductDto>(),
            new List<CouponPerformanceDto>(),
            new SystemHealthDto(),
            new OperationalAlertsDto { CriticalStockAlerts = [] },
            new List<RecentActivityDto>(),
            distribution
        );

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.OrderStatusDistribution.Should().HaveCount(4);
        result.OrderStatusDistribution["Processing"].Should().Be(50);
        result.OrderStatusDistribution["Paid"].Should().Be(300);
        result.OrderStatusDistribution["Cancelled"].Should().Be(20);
        result.OrderStatusDistribution["Failed"].Should().Be(10);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldThrowException_WhenGetSalesMetricsThrows()
    {
        // Arrange
        _mockDashboardRepository.Setup(r => r.GetSalesMetricsAsync())
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var act = async () => await _sut.GetDashboardSummaryAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Database connection failed");

        _mockDashboardRepository.Verify(r => r.GetSalesMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetInventoryMetricsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldThrowException_WhenGetInventoryMetricsThrows()
    {
        // Arrange
        _mockDashboardRepository.Setup(r => r.GetSalesMetricsAsync())
            .ReturnsAsync(new SalesMetricsDto());

        _mockDashboardRepository.Setup(r => r.GetInventoryMetricsAsync())
            .ThrowsAsync(new InvalidOperationException("Invalid query"));

        // Act
        var act = async () => await _sut.GetDashboardSummaryAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid query");

        _mockDashboardRepository.Verify(r => r.GetSalesMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetInventoryMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRevenueTrendAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldThrowException_WhenGetRevenueTrendThrows()
    {
        // Arrange
        _mockDashboardRepository.Setup(r => r.GetSalesMetricsAsync())
            .ReturnsAsync(new SalesMetricsDto());

        _mockDashboardRepository.Setup(r => r.GetInventoryMetricsAsync())
            .ReturnsAsync(new InventoryMetricsDto { LowStockProducts = [] });

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(7))
            .ThrowsAsync(new TimeoutException("Query timeout"));

        // Act
        var act = async () => await _sut.GetDashboardSummaryAsync();

        // Assert
        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("Query timeout");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldThrowException_WhenGetTopSellingProductsThrows()
    {
        // Arrange
        SetupRepositoryUntilRevenueTrend();

        _mockDashboardRepository.Setup(r => r.GetTopSellingProductsAsync(10))
            .ThrowsAsync(new ArgumentException("Invalid parameter"));

        // Act
        var act = async () => await _sut.GetDashboardSummaryAsync();

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid parameter");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldThrowException_WhenGetSystemHealthThrows()
    {
        // Arrange
        SetupRepositoryUntilTopCoupons();

        _mockDashboardRepository.Setup(r => r.GetSystemHealthAsync())
            .ThrowsAsync(new NullReferenceException("Null reference"));

        // Act
        var act = async () => await _sut.GetDashboardSummaryAsync();

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>()
            .WithMessage("Null reference");
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldThrowException_WhenGetOrderStatusDistributionThrows()
    {
        // Arrange
        SetupRepositoryUntilRecentOrders();

        _mockDashboardRepository.Setup(r => r.GetOrderStatusDistributionAsync())
            .ThrowsAsync(new InvalidOperationException("Cannot complete operation"));

        // Act
        var act = async () => await _sut.GetDashboardSummaryAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot complete operation");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldHandle_ZeroSalesMetrics()
    {
        // Arrange
        var salesMetrics = new SalesMetricsDto
        {
            TodayRevenue = 0m,
            MonthlyRevenue = 0m,
            TodayOrdersCount = 0,
            MonthlyOrdersCount = 0,
            AverageOrderValue = 0m,
            PendingOrdersCount = 0,
            PaidOrdersCount = 0,
            CancelledOrdersCount = 0,
            FailedOrdersCount = 0
        };

        SetupMinimalRepositoryMocks(salesMetrics);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.SalesMetrics.TodayRevenue.Should().Be(0m);
        result.SalesMetrics.MonthlyRevenue.Should().Be(0m);
        result.SalesMetrics.TodayOrdersCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldHandle_VeryLargeRevenueValues()
    {
        // Arrange
        var salesMetrics = new SalesMetricsDto
        {
            TodayRevenue = decimal.MaxValue,
            MonthlyRevenue = decimal.MaxValue,
            AverageOrderValue = decimal.MaxValue
        };

        SetupMinimalRepositoryMocks(salesMetrics);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.SalesMetrics.TodayRevenue.Should().Be(decimal.MaxValue);
        result.SalesMetrics.MonthlyRevenue.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldHandle_VeryLargeOrderCounts()
    {
        // Arrange
        var salesMetrics = new SalesMetricsDto
        {
            TodayOrdersCount = int.MaxValue,
            MonthlyOrdersCount = int.MaxValue,
            PendingOrdersCount = int.MaxValue
        };

        SetupMinimalRepositoryMocks(salesMetrics);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.SalesMetrics.TodayOrdersCount.Should().Be(int.MaxValue);
        result.SalesMetrics.MonthlyOrdersCount.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldHandle_EmptyRevenueTrendLists()
    {
        // Arrange
        SetupMinimalRepositoryMocks(
            new SalesMetricsDto(),
            new InventoryMetricsDto { LowStockProducts = [] },
            new List<RevenueTrendDto>(),
            new List<RevenueTrendDto>()
        );

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.Last7DaysTrend.Should().BeEmpty();
        result.Last30DaysTrend.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldHandle_LargeCollections()
    {
        // Arrange
        var largeProductList = Enumerable.Range(1, 1000)
            .Select(i => new TopSellingProductDto
            {
                ProductId = i,
                ProductTitle = $"Product {i}",
                ImageName = $"img{i}.jpg",
                Price = i * 10m,
                TotalQuantitySold = i * 5,
                TotalRevenue = i * 50m
            })
            .ToList();

        SetupMinimalRepositoryMocks(
            new SalesMetricsDto(),
            new InventoryMetricsDto { LowStockProducts = [] },
            new List<RevenueTrendDto>(),
            new List<RevenueTrendDto>(),
            largeProductList
        );

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.TopSellingProducts.Should().HaveCount(1000);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldHandle_NegativeStockLevels()
    {
        // Arrange
        var inventoryMetrics = new InventoryMetricsDto
        {
            TotalProducts = 100,
            LowStockProductsCount = 3,
            LowStockProducts = new List<LowStockProductDto>
            {
                new() { Id = 1, Title = "Product 1", CurrentStock = -5, ImageName = "img1.jpg", StockLevel = StockLevel.Critical },
                new() { Id = 2, Title = "Product 2", CurrentStock = 0, ImageName = "img2.jpg", StockLevel = StockLevel.Critical }
            }
        };

        SetupMinimalRepositoryMocks(new SalesMetricsDto(), inventoryMetrics);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.InventoryMetrics.LowStockProducts[0].CurrentStock.Should().Be(-5);
        result.InventoryMetrics.LowStockProducts[1].CurrentStock.Should().Be(0);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCallAllRepositoryMethods_ExactlyOnce()
    {
        // Arrange
        SetupAllRepositoryMocks(
            new SalesMetricsDto(),
            new InventoryMetricsDto { LowStockProducts = [] },
            new List<RevenueTrendDto>(),
            new List<RevenueTrendDto>(),
            new List<TopSellingProductDto>(),
            new List<CouponPerformanceDto>(),
            new SystemHealthDto(),
            new OperationalAlertsDto { CriticalStockAlerts = [] },
            new List<RecentActivityDto>(),
            new Dictionary<string, int>()
        );

        // Act
        await _sut.GetDashboardSummaryAsync();

        // Assert
        _mockDashboardRepository.Verify(r => r.GetSalesMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetInventoryMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRevenueTrendAsync(7), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRevenueTrendAsync(30), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetTopSellingProductsAsync(10), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetTopCouponsPerformanceAsync(5), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetSystemHealthAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetOperationalAlertsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRecentOrdersAsync(10), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetOrderStatusDistributionAsync(), Times.Once);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldNotCallSubsequentMethods_WhenEarlyMethodThrows()
    {
        // Arrange
        _mockDashboardRepository.Setup(r => r.GetSalesMetricsAsync())
            .ReturnsAsync(new SalesMetricsDto());

        _mockDashboardRepository.Setup(r => r.GetInventoryMetricsAsync())
            .ThrowsAsync(new Exception("Error"));

        // Act
        try
        {
            await _sut.GetDashboardSummaryAsync();
        }
        catch
        {
            // Expected exception
        }

        // Assert
        _mockDashboardRepository.Verify(r => r.GetSalesMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetInventoryMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRevenueTrendAsync(It.IsAny<int>()), Times.Never);
        _mockDashboardRepository.Verify(r => r.GetTopSellingProductsAsync(It.IsAny<int>()), Times.Never);
        _mockDashboardRepository.Verify(r => r.GetSystemHealthAsync(), Times.Never);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldCreateNewDashboardSummaryDto_WithAllProperties()
    {
        // Arrange
        var salesMetrics = CreateSalesMetrics();
        var inventoryMetrics = CreateInventoryMetrics();

        SetupMinimalRepositoryMocks(salesMetrics, inventoryMetrics);

        // Act
        var result = await _sut.GetDashboardSummaryAsync();

        // Assert
        result.Should().BeOfType<DashboardSummaryDto>();
        result.SalesMetrics.Should().NotBeNull();
        result.InventoryMetrics.Should().NotBeNull();
        result.Last7DaysTrend.Should().NotBeNull();
        result.Last30DaysTrend.Should().NotBeNull();
        result.TopSellingProducts.Should().NotBeNull();
        result.TopCoupons.Should().NotBeNull();
        result.SystemHealth.Should().NotBeNull();
        result.OperationalAlerts.Should().NotBeNull();
        result.RecentOrders.Should().NotBeNull();
        result.OrderStatusDistribution.Should().NotBeNull();
    }

    #endregion

    #endregion

    #region Helper Methods

    private static SalesMetricsDto CreateSalesMetrics()
    {
        return new SalesMetricsDto
        {
            TodayRevenue = 2500.50m,
            MonthlyRevenue = 75000.75m,
            TodayOrdersCount = 45,
            MonthlyOrdersCount = 850,
            AverageOrderValue = 88.24m,
            PendingOrdersCount = 30,
            PaidOrdersCount = 750,
            CancelledOrdersCount = 40,
            FailedOrdersCount = 30
        };
    }

    private static InventoryMetricsDto CreateInventoryMetrics()
    {
        return new InventoryMetricsDto
        {
            TotalProducts = 1000,
            LowStockProductsCount = 50,
            LowStockProducts = new List<LowStockProductDto>
            {
                new() { Id = 1, Title = "Product 1", CurrentStock = 2, ImageName = "img1.jpg", StockLevel = StockLevel.Critical },
                new() { Id = 2, Title = "Product 2", CurrentStock = 15, ImageName = "img2.jpg", StockLevel = StockLevel.Low }
            }
        };
    }

    private static List<RevenueTrendDto> CreateRevenueTrend(int days)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
        return Enumerable.Range(0, days)
            .Select(i => new RevenueTrendDto
            {
                Date = startDate.AddDays(i),
                Revenue = (i + 1) * 100m,
                OrdersCount = (i + 1) * 5
            })
            .ToList();
    }

    private static List<TopSellingProductDto> CreateTopSellingProducts(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new TopSellingProductDto
            {
                ProductId = i,
                ProductTitle = $"Product {i}",
                ImageName = $"img{i}.jpg",
                Price = i * 50m,
                TotalQuantitySold = i * 10,
                TotalRevenue = i * 500m
            })
            .ToList();
    }

    private static List<CouponPerformanceDto> CreateCouponPerformance(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new CouponPerformanceDto
            {
                CouponId = i,
                CouponCode = $"COUPON{i}",
                DiscountPercentage = i * 5,
                UsageCount = i * 20,
                TotalDiscountGiven = i * 100m,
                TotalRevenueWithCoupon = i * 1000m
            })
            .ToList();
    }

    private static SystemHealthDto CreateSystemHealth()
    {
        return new SystemHealthDto
        {
            FailedPaymentsCount = 5,
            PendingPaymentsCount = 15,
            ActiveReservationsCount = 30,
            ExpiredReservationsCount = 100
        };
    }

    private static OperationalAlertsDto CreateOperationalAlerts()
    {
        return new OperationalAlertsDto
        {
            CriticalStockAlerts = new List<LowStockProductDto>
            {
                new() { Id = 1, Title = "Critical Product", CurrentStock = 1, ImageName = "img.jpg", StockLevel = StockLevel.Critical }
            },
            FailedOrdersLast24Hours = 10,
            PendingPaymentsCount = 20
        };
    }

    private static List<RecentActivityDto> CreateRecentOrders(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new RecentActivityDto
            {
                OrderId = i,
                OrderNumber = $"ORD-{i:D6}",
                CustomerName = $"Customer {i}",
                Total = i * 100m,
                Status = "Paid",
                PaymentMethod = "Stripe",
                CreatedAt = DateTime.UtcNow.AddHours(-i)
            })
            .ToList();
    }

    private static Dictionary<string, int> CreateOrderStatusDistribution()
    {
        return new Dictionary<string, int>
        {
            { "Processing", 100 },
            { "Paid", 800 },
            { "Cancelled", 50 },
            { "Failed", 30 }
        };
    }

    private void SetupAllRepositoryMocks(
        SalesMetricsDto salesMetrics,
        InventoryMetricsDto inventoryMetrics,
        List<RevenueTrendDto> last7DaysTrend,
        List<RevenueTrendDto> last30DaysTrend,
        List<TopSellingProductDto> topSellingProducts,
        List<CouponPerformanceDto> topCoupons,
        SystemHealthDto systemHealth,
        OperationalAlertsDto operationalAlerts,
        List<RecentActivityDto> recentOrders,
        Dictionary<string, int> orderStatusDistribution)
    {
        _mockDashboardRepository.Setup(r => r.GetSalesMetricsAsync())
            .ReturnsAsync(salesMetrics);

        _mockDashboardRepository.Setup(r => r.GetInventoryMetricsAsync())
            .ReturnsAsync(inventoryMetrics);

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(7))
            .ReturnsAsync(last7DaysTrend);

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(30))
            .ReturnsAsync(last30DaysTrend);

        _mockDashboardRepository.Setup(r => r.GetTopSellingProductsAsync(10))
            .ReturnsAsync(topSellingProducts);

        _mockDashboardRepository.Setup(r => r.GetTopCouponsPerformanceAsync(5))
            .ReturnsAsync(topCoupons);

        _mockDashboardRepository.Setup(r => r.GetSystemHealthAsync())
            .ReturnsAsync(systemHealth);

        _mockDashboardRepository.Setup(r => r.GetOperationalAlertsAsync())
            .ReturnsAsync(operationalAlerts);

        _mockDashboardRepository.Setup(r => r.GetRecentOrdersAsync(10))
            .ReturnsAsync(recentOrders);

        _mockDashboardRepository.Setup(r => r.GetOrderStatusDistributionAsync())
            .ReturnsAsync(orderStatusDistribution);
    }

    private void SetupMinimalRepositoryMocks(
        SalesMetricsDto? salesMetrics = null,
        InventoryMetricsDto? inventoryMetrics = null,
        List<RevenueTrendDto>? last7DaysTrend = null,
        List<RevenueTrendDto>? last30DaysTrend = null,
        List<TopSellingProductDto>? topSellingProducts = null,
        List<CouponPerformanceDto>? topCoupons = null,
        SystemHealthDto? systemHealth = null,
        OperationalAlertsDto? operationalAlerts = null,
        List<RecentActivityDto>? recentOrders = null,
        Dictionary<string, int>? orderStatusDistribution = null)
    {
        _mockDashboardRepository.Setup(r => r.GetSalesMetricsAsync())
            .ReturnsAsync(salesMetrics ?? new SalesMetricsDto());

        _mockDashboardRepository.Setup(r => r.GetInventoryMetricsAsync())
            .ReturnsAsync(inventoryMetrics ?? new InventoryMetricsDto { LowStockProducts = [] });

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(7))
            .ReturnsAsync(last7DaysTrend ?? new List<RevenueTrendDto>());

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(30))
            .ReturnsAsync(last30DaysTrend ?? new List<RevenueTrendDto>());

        _mockDashboardRepository.Setup(r => r.GetTopSellingProductsAsync(10))
            .ReturnsAsync(topSellingProducts ?? new List<TopSellingProductDto>());

        _mockDashboardRepository.Setup(r => r.GetTopCouponsPerformanceAsync(5))
            .ReturnsAsync(topCoupons ?? new List<CouponPerformanceDto>());

        _mockDashboardRepository.Setup(r => r.GetSystemHealthAsync())
            .ReturnsAsync(systemHealth ?? new SystemHealthDto());

        _mockDashboardRepository.Setup(r => r.GetOperationalAlertsAsync())
            .ReturnsAsync(operationalAlerts ?? new OperationalAlertsDto { CriticalStockAlerts = [] });

        _mockDashboardRepository.Setup(r => r.GetRecentOrdersAsync(10))
            .ReturnsAsync(recentOrders ?? new List<RecentActivityDto>());

        _mockDashboardRepository.Setup(r => r.GetOrderStatusDistributionAsync())
            .ReturnsAsync(orderStatusDistribution ?? new Dictionary<string, int>());
    }

    private void SetupRepositoryUntilRevenueTrend()
    {
        _mockDashboardRepository.Setup(r => r.GetSalesMetricsAsync())
            .ReturnsAsync(new SalesMetricsDto());

        _mockDashboardRepository.Setup(r => r.GetInventoryMetricsAsync())
            .ReturnsAsync(new InventoryMetricsDto { LowStockProducts = [] });

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(7))
            .ReturnsAsync(new List<RevenueTrendDto>());

        _mockDashboardRepository.Setup(r => r.GetRevenueTrendAsync(30))
            .ReturnsAsync(new List<RevenueTrendDto>());
    }

    private void SetupRepositoryUntilTopCoupons()
    {
        SetupRepositoryUntilRevenueTrend();

        _mockDashboardRepository.Setup(r => r.GetTopSellingProductsAsync(10))
            .ReturnsAsync(new List<TopSellingProductDto>());

        _mockDashboardRepository.Setup(r => r.GetTopCouponsPerformanceAsync(5))
            .ReturnsAsync(new List<CouponPerformanceDto>());
    }

    private void SetupRepositoryUntilRecentOrders()
    {
        SetupRepositoryUntilTopCoupons();

        _mockDashboardRepository.Setup(r => r.GetSystemHealthAsync())
            .ReturnsAsync(new SystemHealthDto());

        _mockDashboardRepository.Setup(r => r.GetOperationalAlertsAsync())
            .ReturnsAsync(new OperationalAlertsDto { CriticalStockAlerts = [] });

        _mockDashboardRepository.Setup(r => r.GetRecentOrdersAsync(10))
            .ReturnsAsync(new List<RecentActivityDto>());
    }

    private void VerifyAllRepositoryMethodsCalled()
    {
        _mockDashboardRepository.Verify(r => r.GetSalesMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetInventoryMetricsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRevenueTrendAsync(7), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRevenueTrendAsync(30), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetTopSellingProductsAsync(10), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetTopCouponsPerformanceAsync(5), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetSystemHealthAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetOperationalAlertsAsync(), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetRecentOrdersAsync(10), Times.Once);
        _mockDashboardRepository.Verify(r => r.GetOrderStatusDistributionAsync(), Times.Once);
    }

    #endregion
}
