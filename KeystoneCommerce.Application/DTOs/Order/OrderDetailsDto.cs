namespace KeystoneCommerce.Application.DTOs.Order;

public class OrderDetailsDto
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

    public UserBasicInfoDto User { get; set; } = null!;
    public ShippingAddressDetailsDto ShippingAddress { get; set; } = null!;
    public ShippingMethodDetailsDto ShippingMethod { get; set; } = null!;
    public PaymentDetailsDto? Payment { get; set; }
    public CouponDetailsDto? Coupon { get; set; }
    public List<OrderItemDetailsDto> OrderItems { get; set; } = [];
}
