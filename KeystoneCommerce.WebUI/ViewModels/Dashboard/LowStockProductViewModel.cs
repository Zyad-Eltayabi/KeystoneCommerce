namespace KeystoneCommerce.WebUI.ViewModels.Dashboard;

public class LowStockProductViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int CurrentStock { get; set; }
    public string ImageName { get; set; } = null!;
    public string StockLevel { get; set; } = null!;
}
