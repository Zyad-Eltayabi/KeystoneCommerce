using KeystoneCommerce.Application.DTOs.Coupon;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface ICouponService
    {
        Task<int> GetDiscountPercentageByCodeAsync(string promoCode);
        Task<Result<CouponDto>> GetCouponByName(string couponName);
    }
}
