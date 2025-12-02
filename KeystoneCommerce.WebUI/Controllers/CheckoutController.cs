using AutoMapper;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Services;
using KeystoneCommerce.WebUI.ViewModels.Cart;
using KeystoneCommerce.WebUI.ViewModels.Checkout;
using KeystoneCommerce.WebUI.ViewModels.ShippingMethod;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class CheckoutController(CartService cartService, ICouponService couponService, IShippingMethodService shippingMethodService, IMapper mapper) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        CreateCheckoutViewModel viewModel = await BuildCheckoutViewModel();
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

        var couponViewModel = new ApplyCouponViewModel()
        {
            CouponCode = couponCode,
            DiscountAmount = couponDiscountPercentage,
        };
        var viewModel = await BuildCheckoutViewModel(couponViewModel);
        return View("Index", viewModel);
    }

    private async Task<CreateCheckoutViewModel> BuildCheckoutViewModel(ApplyCouponViewModel? couponViewModel = null)
    {
        return new CreateCheckoutViewModel()
        {
            Products = await GetProductCartViewModels(),
            ShippingMethods = await GetShippingMethodViewModels(),
            ApplyCoupon = couponViewModel ?? new ApplyCouponViewModel()
        };
    }

    private async Task<List<ProductCartViewModel>?> GetProductCartViewModels()
        => await cartService.GetProductCartViewModels();

    private async Task<List<ShippingMethodViewModel>> GetShippingMethodViewModels()
    {
        var shippingMethods = await shippingMethodService.GetAllShippingMethodsAsync();
        if (shippingMethods is null || !shippingMethods.Any())
            return new List<ShippingMethodViewModel>();
        return mapper.Map<List<ShippingMethodViewModel>>(shippingMethods);
    }
}