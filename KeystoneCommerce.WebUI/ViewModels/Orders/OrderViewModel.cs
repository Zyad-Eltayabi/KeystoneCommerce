namespace KeystoneCommerce.WebUI.ViewModels.Orders;

public class OrderViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
    public decimal Shipping { get; set; }
    public decimal Discount { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
