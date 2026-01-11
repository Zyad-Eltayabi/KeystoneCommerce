namespace KeystoneCommerce.WebUI.ViewModels.Dashboard;

public class OperationalAlertsViewModel
{
    public List<LowStockProductViewModel> CriticalStockAlerts { get; set; } = [];
    public int FailedOrdersLast24Hours { get; set; }
    public int PendingPaymentsCount { get; set; }
}
