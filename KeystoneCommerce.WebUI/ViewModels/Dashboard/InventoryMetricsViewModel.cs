namespace KeystoneCommerce.WebUI.ViewModels.Dashboard;

public class InventoryMetricsViewModel
{
    public int LowStockProductsCount { get; set; }
    public int TotalProducts { get; set; }
    public List<LowStockProductViewModel> LowStockProducts { get; set; } = [];
}
