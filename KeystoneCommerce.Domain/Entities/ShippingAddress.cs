namespace KeystoneCommerce.Domain.Entities;

public class ShippingAddress
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PostalCode { get; set; }
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get;private set; } = DateTime.UtcNow;
    public Order Order { get; set; } = null!;
}