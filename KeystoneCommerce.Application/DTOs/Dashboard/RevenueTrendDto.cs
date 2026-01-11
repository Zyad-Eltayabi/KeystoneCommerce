namespace KeystoneCommerce.Application.DTOs.Dashboard;

public class RevenueTrendDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrdersCount { get; set; }
}
