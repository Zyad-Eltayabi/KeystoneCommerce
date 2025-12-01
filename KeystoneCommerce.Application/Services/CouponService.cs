using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;

namespace KeystoneCommerce.Application.Services
{
    public class CouponService(ICouponRepository couponRepository) : ICouponService
    {
        public async Task<int> GetDiscountPercentageByCodeAsync(string promoCode = "0")
        {
            var coupon = await couponRepository.FindAsync(c => c.Code == promoCode);
            if (coupon is null || !coupon.IsActive)
                return 0;
            return coupon.DiscountPercentage;
        }
    }
}
