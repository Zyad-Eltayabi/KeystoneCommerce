namespace KeystoneCommerce.Application.DTOs.Dashboard;

public class TopSellingProductDto
{
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = null!;
    public string ImageName { get; set; } = null!;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal Price { get; set; }
}
