using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Services;
using KeystoneCommerce.WebUI.ViewModels.Cart;
using KeystoneCommerce.WebUI.ViewModels.Checkout;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class CheckoutController(CartService cartService, ICouponService couponService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var viewModel = new CreateCheckoutViewModel()
        {
            Products = await cartService.GetProductCartViewModels()
        };
        return View("Index", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string couponCode)
    {
        var couponDiscountPercentage = await couponService.GetDiscountPercentageByCodeAsync(couponCode);
        if (couponDiscountPercentage == 0)
            TempData["ErrorMessage"] = "Invalid coupon code.";
        else
            TempData["Success"] = $"Coupon applied! You received a {couponDiscountPercentage}% discount.";
        var viewModel = new CreateCheckoutViewModel()
            {
                Products = await GetProductCartViewModels(),
                ApplyCoupon = new ApplyCouponViewModel()
                {
                    CouponCode = couponCode,
                    DiscountAmount = couponDiscountPercentage
                }
            };
        return View("Index", viewModel);
    }

    private async Task<List<ProductCartViewModel>?> GetProductCartViewModels()
        => await cartService.GetProductCartViewModels();
}