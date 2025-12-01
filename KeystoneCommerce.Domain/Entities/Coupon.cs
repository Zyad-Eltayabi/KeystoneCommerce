using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Domain.Entities;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public int DiscountPercentage { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive => EndAt >= DateTime.UtcNow;
    public DateTime CreatedAt { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}