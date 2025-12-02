using KeystoneCommerce.WebUI.ViewModels.Cart;
using KeystoneCommerce.WebUI.ViewModels.ShippingMethod;

namespace KeystoneCommerce.WebUI.ViewModels.Checkout;

public class CreateCheckoutViewModel
{
    public List<ProductCartViewModel>? Products { get; set; } = new();
    public ApplyCouponViewModel ApplyCoupon { get; set; } = new();
    public List<ShippingMethodViewModel> ShippingMethods { get; set; } = new();
}