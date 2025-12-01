using KeystoneCommerce.WebUI.ViewModels.Cart;

namespace KeystoneCommerce.WebUI.ViewModels.Checkout;

public class CreateCheckoutViewModel
{
    public List<ProductCartViewModel>? Products { get; set; } = new();
    public ApplyCouponViewModel ApplyCoupon { get; set; } = new();
}