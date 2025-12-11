using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace KeystoneCommerce.WebUI.Controllers
{
    public class PaymentController : Controller
    {
        private readonly CartService _cartService;
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly IConfiguration _configuration;
        public PaymentController(
            CartService cartService,
            IPaymentGatewayService paymentGatewayService,
            IConfiguration configuration)
        {
            _cartService = cartService;
            _paymentGatewayService = paymentGatewayService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> CreateCheckoutSession(decimal totalPrice)
        {
            var productCartViewModels = await _cartService.GetProductCartViewModels();
            if (productCartViewModels == null || !productCartViewModels.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }
            var baseUrl = _configuration["AppSettings:BaseUrl"];
            var sessionDto = new CreatePaymentSessionDto
            {
                LineItems = productCartViewModels.Select(item => new PaymentLineItemDto
                {
                    ProductName = item.Title,
                    Quantity = item.Count
                }).ToList(),
                TotalPrice = totalPrice,
                SuccessUrl = $"{baseUrl}/payment/success",
                CancelUrl = $"{baseUrl}/payment/cancel"
            };
            var result = await _paymentGatewayService.CreatePaymentSessionAsync(sessionDto);
            Response.Headers.Append("Location", result.PaymentUrl);
            return new StatusCodeResult(303);
        }

        public IActionResult Success()
        {
            return Content("Payment succeeded!");
        }

        public IActionResult Cancel()
        {
            return Content("Payment canceled.");
        }
    }

    public static class StripeSessionStore
    {
        public static HashSet<string> Sessions = [];
    }
}
