using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; } // sum of item line totals before shipping/discount.
    public decimal Total { get; set; }
    public decimal Shipping { get; set; }
    public decimal Discount { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public int ShippingAddressId { get; set; }
    public int ShippingMethodId { get; set; }
    public string UserId { get; set; } = null!;
    public int? CouponId { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; } 
    public Coupon? Coupon { get; set; }
    public ShippingMethod ShippingMethod { get; set; } = null!;
    public ShippingAddress ShippingAddress { get; set; } = null!;
    public InventoryReservation? InventoryReservation { get; set; }
}