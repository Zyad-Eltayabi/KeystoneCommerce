namespace KeystoneCommerce.WebUI.ViewModels.Dashboard;

public class CouponPerformanceViewModel
{
    public int CouponId { get; set; }
    public string CouponCode { get; set; } = null!;
    public int UsageCount { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal TotalRevenueWithCoupon { get; set; }
    public int DiscountPercentage { get; set; }
}
