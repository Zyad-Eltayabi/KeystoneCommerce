using AutoMapper;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

[Route("Admin/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IMapper _mapper;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        IMapper mapper,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("Loading admin dashboard");
            
            var dashboardData = await _dashboardService.GetDashboardSummaryAsync();
            var viewModel = _mapper.Map<DashboardViewModel>(dashboardData);
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            TempData["ErrorMessage"] = "Failed to load dashboard data. Please try again.";
            return View(GetEmptyDashboardViewModel());
        }
    }

    private static DashboardViewModel GetEmptyDashboardViewModel()
    {
        return new DashboardViewModel
        {
            SalesMetrics = new SalesMetricsViewModel(),
            InventoryMetrics = new InventoryMetricsViewModel(),
            Last7DaysTrend = new List<RevenueTrendViewModel>(),
            Last30DaysTrend = new List<RevenueTrendViewModel>(),
            TopSellingProducts = new List<TopSellingProductViewModel>(),
            TopCoupons = new List<CouponPerformanceViewModel>(),
            SystemHealth = new SystemHealthViewModel(),
            OperationalAlerts = new OperationalAlertsViewModel(),
            RecentOrders = new List<RecentOrderViewModel>(),
            OrderStatusDistribution = new Dictionary<string, int>()
        };
    }
}
