using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Payment;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IPaymentGatewayService
    {
        Task<Result<PaymentSessionResultDto>> CreatePaymentSessionAsync(CreatePaymentSessionDto sessionDto);
        Task<Result<bool>> ConfirmPaymentAndUpdateOrderAsync(ConfirmPaymentDto confirmPaymentDto);
    }
}
