namespace KeystoneCommerce.WebUI.ViewModels.Orders;

public class OrderAnalyticsViewModel
{
    public int OrderCount { get; set; }
    public decimal OrdersRevenue { get; set; }
    public int PendingOrdersCount { get; set; }
    public int FailedOrdersCount { get; set; }
    public int CancellationOrdersCount { get; set; }
    public decimal AverageOrderValue { get; set; }
}
