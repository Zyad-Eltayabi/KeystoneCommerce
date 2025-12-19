using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace KeystoneCommerce.Infrastructure.Services
{
    public class StripPaymentService : IStripPaymentService
    {

        public StripPaymentService(IConfiguration configuration)
        {
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
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
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