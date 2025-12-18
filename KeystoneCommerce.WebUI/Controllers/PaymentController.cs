using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Services;
using KeystoneCommerce.WebUI.ViewModels.Payment;
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
        private readonly ILogger<PaymentController> _logger;
        private readonly CartCookieService _cartCookieService;
        public PaymentController(
            IPaymentGatewayService paymentGatewayService,
            IConfiguration configuration,
            ILogger<PaymentController> logger,
            CartCookieService cartCookieService)
        {
            _paymentGatewayService = paymentGatewayService;
            _configuration = configuration;
            _logger = logger;
            _cartCookieService = cartCookieService;
        }

        [HttpGet]
        public async Task<IActionResult> CreateCheckoutSession(decimal totalPrice, int paymentId)
        {
            var baseUrl = _configuration["AppSettings:BaseUrl"];
            var sessionDto = new CreatePaymentSessionDto
            {
                TotalPrice = totalPrice,
                PaymentId = paymentId,
                SuccessUrl = $"{baseUrl}/payment/success?paymentId={paymentId}",
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

        public async Task<IActionResult> Success(int paymentId)
        {
            string orderNumber = await _paymentGatewayService.GetOrderNumberByPaymentId(paymentId);

            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                _logger.LogWarning("Order number not found for payment ID: {PaymentId}", paymentId);
                TempData["ErrorMessage"] = "Order not found for the completed payment.";
                return RedirectToAction("Index", "Home");
            }

            _cartCookieService.ClearCart();

            var viewModel = new PaymentSuccessViewModel
            {
                OrderNumber = orderNumber
            };

            return View(viewModel);

        }

        [HttpGet]
        public IActionResult Cancel()
        {
            return RedirectToAction("Index", "Checkout");
        }

        [HttpPost]
        [Route("payment/webhooks/stripe")]
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
                await HandleStripeEventAsync(stripeEvent);
                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe web hook error: {Message}", ex.Message);
                return BadRequest();
            }
        }

        private async Task HandleStripeEventAsync(Event stripeEvent)
        {
            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                case EventTypes.CheckoutSessionAsyncPaymentSucceeded:
                    await HandleSuccessfulPaymentAsync(stripeEvent);
                    break;

                case EventTypes.CheckoutSessionExpired:
                    await HandleCancelledPaymentAsync(stripeEvent);
                    break;

                case EventTypes.PaymentIntentPaymentFailed:
                    await HandlePaymentIntentFailureAsync(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }

        private async Task HandleSuccessfulPaymentAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session is null)
            {
                _logger.LogWarning("Failed to parse session from successful payment event");
                return;
            }

            _logger.LogInformation("Processing successful payment for session: {SessionId}", session.Id);
            await FulfillCheckout(session.Id);
        }

        private async Task HandleCancelledPaymentAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session is null)
            {
                _logger.LogWarning("Failed to parse session from cancelled payment event");
                return;
            }

            _logger.LogWarning("Payment cancelled or expired for session: {SessionId}, Event Type: {EventType}",
                session.Id, stripeEvent.Type);

            if (session.Metadata == null || !session.Metadata.ContainsKey("PaymentId_DB"))
            {
                _logger.LogError("PaymentId_DB metadata not found in session: {SessionId}", session.Id);
                return;
            }

            var paymentId = int.Parse(session.Metadata["PaymentId_DB"].ToString());
            var cancelPaymentDto = new CancelPaymentDto
            {
                PaymentId = paymentId,
                ProviderTransactionId = session.PaymentIntentId ?? session.Id
            };

            var result = await _paymentGatewayService.CancelPaymentAndUpdateOrderAsync(cancelPaymentDto);
            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to mark payment and order as cancelled. Payment ID: {PaymentId}. Errors: {Errors}",
                    paymentId, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("Successfully marked payment and order as cancelled. Payment ID: {PaymentId}",
                    paymentId);
            }
        }

        private async Task HandlePaymentIntentFailureAsync(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent is null)
            {
                _logger.LogWarning("Failed to parse PaymentIntent from event");
                return;
            }

            _logger.LogWarning("PaymentIntent failed: {PaymentIntentId}, Event Type: {EventType}",
                paymentIntent.Id, stripeEvent.Type);

            if (paymentIntent.Metadata == null || !paymentIntent.Metadata.ContainsKey("PaymentId_DB"))
            {
                _logger.LogError("PaymentId_DB metadata not found in PaymentIntent: {PaymentIntentId}", paymentIntent.Id);
                return;
            }

            var paymentId = int.Parse(paymentIntent.Metadata["PaymentId_DB"].ToString());
            var failPaymentDto = new FailPaymentDto
            {
                PaymentId = paymentId,
                ProviderTransactionId = paymentIntent.Id
            };

            var result = await _paymentGatewayService.FailPaymentAndUpdateOrderAsync(failPaymentDto);
            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to mark payment and order as failed. Payment ID: {PaymentId}. Errors: {Errors}",
                    paymentId, string.Join(", ", result.Errors));
            }
            else
            {
                _logger.LogInformation("Successfully marked payment and order as failed due to PaymentIntent failure. Payment ID: {PaymentId}",
                    paymentId);
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
