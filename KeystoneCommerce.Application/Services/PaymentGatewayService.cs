using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Services;

namespace KeystoneCommerce.Application.Services
{
    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly IStripPaymentService _stripPaymentService;

        public PaymentGatewayService(IStripPaymentService stripPaymentService)
        {
            _stripPaymentService = stripPaymentService;
        }

        public async Task<PaymentSessionResultDto> CreatePaymentSessionAsync(CreatePaymentSessionDto sessionDto)
        {
            return await _stripPaymentService.CreatePaymentSessionAsync(sessionDto);
        }
    }
}