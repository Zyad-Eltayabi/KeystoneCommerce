using KeystoneCommerce.WebUI.ViewModels.Cart;
using KeystoneCommerce.WebUI.ViewModels.ShippingMethod;

namespace KeystoneCommerce.WebUI.ViewModels.Checkout;

public class CreateCheckoutViewModel
{
    public List<ProductCartViewModel>? Products { get; set; } = new();
    public ApplyCouponViewModel ApplyCoupon { get; set; } = new();
    public List<ShippingMethodViewModel> ShippingMethods { get; set; } = new();
    public ShippingDetailsViewModel ShippingDetails { get; set; } = new();
    public string ShippingMethod { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public int PaymentType { get; set; }
}