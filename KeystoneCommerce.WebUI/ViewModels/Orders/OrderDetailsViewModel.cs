using KeystoneCommerce.WebUI.ViewModels.Account;
using KeystoneCommerce.WebUI.ViewModels.Coupon;
using KeystoneCommerce.WebUI.ViewModels.OrderItem;
using KeystoneCommerce.WebUI.ViewModels.Payment;
using KeystoneCommerce.WebUI.ViewModels.ShippingAddress;
using KeystoneCommerce.WebUI.ViewModels.ShippingMethod;

namespace KeystoneCommerce.WebUI.ViewModels.Orders;

public class OrderDetailsViewModel
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

    public UserBasicViewModel User { get; set; } = null!;
    public ShippingAddressViewModel ShippingAddress { get; set; } = null!;
    public OrderShippingMethodViewModel ShippingMethod { get; set; } = null!;
    public PaymentViewModel? Payment { get; set; }
    public CouponViewModel? Coupon { get; set; }
    public List<OrderItemViewModel> OrderItems { get; set; } = new();
}
