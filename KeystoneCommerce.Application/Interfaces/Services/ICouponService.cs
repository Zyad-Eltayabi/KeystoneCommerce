namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface ICouponService
    {
        Task<int> GetDiscountPercentageByCodeAsync(string promoCode);
    }
}
