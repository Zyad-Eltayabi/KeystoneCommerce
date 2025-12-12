using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Threading.Tasks;

namespace KeystoneCommerce.WebUI.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly IConfiguration _configuration;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        public PaymentController(
            IPaymentGatewayService paymentGatewayService,
            IConfiguration configuration,
            IPaymentService paymentService,
            ILogger<PaymentController> logger)
        {
            _paymentGatewayService = paymentGatewayService;
            _configuration = configuration;
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> CreateCheckoutSession(decimal totalPrice, int paymentId)
        {
            var baseUrl = _configuration["AppSettings:BaseUrl"];
            var sessionDto = new CreatePaymentSessionDto
            {
                TotalPrice = totalPrice,
                PaymentId = paymentId,
                SuccessUrl = $"{baseUrl}/payment/success",
                CancelUrl = $"{baseUrl}/payment/cancel"
            };
            var result = await _paymentGatewayService.CreatePaymentSessionAsync(sessionDto);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = "Failed to process payment, pleases try again later.";
                return RedirectToAction("Index", "Checkout");
            }
            Response.Headers.Append("Location", result.Data!.PaymentUrl);
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

        [HttpPost]
        [Route("webhooks/stripe")]
        public async Task<IActionResult> StripeWebhook()
        {
            var secret = _configuration["StripeSettings:WebhookSecret"];
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                  json,
                  Request.Headers["Stripe-Signature"],
                  secret
                );

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted ||
                  stripeEvent.Type == EventTypes.CheckoutSessionAsyncPaymentSucceeded)
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session is null)
                        return BadRequest();
                    await FulfillCheckout(session.Id);
                }
                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe web hook error: {Message}", ex.Message);
                return BadRequest();
            }
        }

        private async Task FulfillCheckout(string sessionId)
        {
            StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
            var service = new SessionService();
            var checkoutSession = service.Get(sessionId, new() { Expand = ["line_items"] });
            if (checkoutSession.PaymentStatus == "paid")
            {
                ConfirmPaymentDto confirm = new()
                {
                    Amount = (decimal)(checkoutSession.AmountTotal / 100m)!,
                    PaymentId = int.Parse(checkoutSession.Metadata["PaymentId_DB"].ToString()),
                    ProviderTransactionId = checkoutSession.PaymentIntentId,
                };
                await _paymentGatewayService.ConfirmPaymentAndUpdateOrderAsync(confirm);
            }
        }
    }
}
