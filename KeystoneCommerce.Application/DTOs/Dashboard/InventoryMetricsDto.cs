namespace KeystoneCommerce.Application.DTOs.Dashboard;

public class InventoryMetricsDto
{
    public int LowStockProductsCount { get; set; }
    public int TotalProducts { get; set; }
    public List<LowStockProductDto> LowStockProducts { get; set; } = [];
}

public class LowStockProductDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int CurrentStock { get; set; }
    public string ImageName { get; set; } = null!;
    public StockLevel StockLevel { get; set; }
}

public enum StockLevel
{
    Critical = 1,   // Stock < 5
    Low = 2,        // Stock < 20
    Warning = 3     // Stock < 50
}
