namespace KeystoneCommerce.Domain.Entities;

public class ShippingMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}