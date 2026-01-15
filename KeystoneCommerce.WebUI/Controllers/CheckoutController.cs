using AutoMapper;
using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.ShippingDetails;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.WebUI.Services;
using KeystoneCommerce.WebUI.ViewModels.Cart;
using KeystoneCommerce.WebUI.ViewModels.Checkout;
using KeystoneCommerce.WebUI.ViewModels.ShippingMethod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KeystoneCommerce.WebUI.Controllers;

[Authorize]
public class CheckoutController(CartService cartService, ICouponService couponService, IShippingMethodService shippingMethodService, IMapper mapper, ICheckoutService checkoutService) : Controller
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCheckOut(CreateCheckoutViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please correct the errors in the form.";
            return RedirectToAction("Index", model);
        }

        var productCartViewModels = await cartService.GetProductCartViewModels();
        if (productCartViewModels == null || !productCartViewModels.Any())
        {
            TempData["ErrorMessage"] = "Your cart is empty.";
            return RedirectToAction("Index", model);
        }

        // Process other payment methods or complete order
        var order = new CreateOrderDto()
        {
            ShippingMethod = model.ShippingMethod,
            ShippingDetails = mapper.Map<CreateShippingDetailsDto>(model.ShippingDetails),
            Coupon = model.CouponCode,
            UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "Anonymous",
            PaymentProvider = model.PaymentProvider
        };

        foreach (var item in productCartViewModels)
        {
            order.ProductsWithQuantity.Add(item.Id, item.Count);
        }

        var processResult = await checkoutService.SubmitOrder(order);
        if (!processResult.IsSuccess)
        {
            TempData["ErrorMessage"] = string.Join(",", processResult.Errors);
            return RedirectToAction("Index", model);
        }

        if (model.PaymentProvider != "CashOnDelivery")
        {
            return RedirectToAction("CreateCheckoutSession", "Payment", new
            {
                totalPrice = processResult.Data!.Total,
                paymentId = processResult.Data.PaymentId
            });
        }

        return RedirectToAction("Success", "Payment", new { paymentId = processResult.Data?.PaymentId ?? 0 });
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