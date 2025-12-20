namespace KeystoneCommerce.WebUI.ViewModels.Checkout;

public class ApplyCouponViewModel
{
    public string CouponCode { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; } = 0;
}