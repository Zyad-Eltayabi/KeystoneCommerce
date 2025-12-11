using KeystoneCommerce.Application.DTOs.Payment;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IPaymentGatewayService
    {
        Task<PaymentSessionResultDto> CreatePaymentSessionAsync(CreatePaymentSessionDto sessionDto);
    }
}
