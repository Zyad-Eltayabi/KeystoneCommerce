using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace KeystoneCommerce.Infrastructure.Services
{
    public class StripPaymentService : IStripPaymentService
    {
        private readonly string _stripeApiKey;

        public StripPaymentService(IConfiguration configuration)
        {
            _stripeApiKey = configuration["StripeSettings:SecretKey"]
                ?? throw new InvalidOperationException("Stripe API key not configured");
            StripeConfiguration.ApiKey = _stripeApiKey;
        }

        public async Task<PaymentSessionResultDto> CreatePaymentSessionAsync(CreatePaymentSessionDto sessionDto)
        {
            var lineItems = new List<SessionLineItemOptions>()
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmountDecimal = (long)(sessionDto.TotalPrice * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Order Total",
                        Description = "Total amount including products, shipping, and discounts",
                    },

                },
                Quantity = 1,
                }
            };

            var options = new SessionCreateOptions
            {
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = sessionDto.SuccessUrl,
                CancelUrl = sessionDto.CancelUrl,
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                Metadata = new Dictionary<string, string>
                {
                    { "PaymentId_DB", sessionDto.PaymentId.ToString() },
                },
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            return new PaymentSessionResultDto
            {
                SessionId = session.Id,
                PaymentUrl = session.Url,
                PaymentId = sessionDto.PaymentId,
            };
        }
    }
}