using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Coupon;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepository;
        private readonly IMappingService _mappingService;
        private readonly ILogger<CouponService> _logger;

        public CouponService(
            ICouponRepository couponRepository,
            IMappingService mappingService,
            ILogger<CouponService> logger)
        {
            _couponRepository = couponRepository;
            _mappingService = mappingService;
            _logger = logger;
        }

        public async Task<int> GetDiscountPercentageByCodeAsync(string promoCode = "0")
        {
            var coupon = await _couponRepository.FindAsync(c => c.Code == promoCode);
            if (coupon is null || !coupon.IsActive)
                return 0;
            return coupon.DiscountPercentage;
        }

        public async Task<Result<CouponDto>> GetCouponByName(string couponName)
        {
            _logger.LogInformation("Fetching coupon with code: {CouponCode}", couponName);

            if (string.IsNullOrWhiteSpace(couponName))
            {
                _logger.LogWarning("Coupon code validation failed - empty or null code provided");
                return Result<CouponDto>.Failure("Coupon code cannot be empty.");
            }

            var coupon = await _couponRepository.FindAsync(c => c.Code == couponName);

            if (coupon is null)
            {
                _logger.LogWarning("Coupon not found with code: {CouponCode}", couponName);
                return Result<CouponDto>.Failure("Coupon not found.");
            }

            if (!coupon.IsActive)
            {
                _logger.LogWarning(
                    "Coupon with code {CouponCode} is inactive. End date: {EndDate}",
                    couponName,
                    coupon.EndAt);
                return Result<CouponDto>.Failure("This coupon has expired.");
            }

            var couponDto = _mappingService.Map<CouponDto>(coupon);

            _logger.LogInformation(
                "Coupon retrieved successfully: Code {CouponCode}, Discount {DiscountPercentage}%",
                coupon.Code,
                coupon.DiscountPercentage);

            return Result<CouponDto>.Success(couponDto);
        }
    }
}
