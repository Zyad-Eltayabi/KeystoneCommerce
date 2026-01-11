namespace KeystoneCommerce.Application.DTOs.Dashboard;

public class OperationalAlertsDto
{
    public List<LowStockProductDto> CriticalStockAlerts { get; set; } = [];
    public int FailedOrdersLast24Hours { get; set; }
    public int PendingPaymentsCount { get; set; }
}
