namespace KeystoneCommerce.Domain.Entities;

public class ShippingMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string EstimatedDays { get; set; } = string.Empty;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}