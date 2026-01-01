namespace KeystoneCommerce.WebUI.ViewModels.Coupon
{
    public class CouponViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public int DiscountPercentage { get; set; }
    }
}
