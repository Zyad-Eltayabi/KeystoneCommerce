namespace KeystoneCommerce.Domain.Entities;
public class OrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}