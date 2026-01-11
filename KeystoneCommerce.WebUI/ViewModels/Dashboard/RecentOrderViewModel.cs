namespace KeystoneCommerce.WebUI.ViewModels.Dashboard;

public class RecentOrderViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public decimal Total { get; set; }
    public string Status { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
