using KeystoneCommerce.Application.DTOs.Payment;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IPaymentGatewayService
    {
        Task<string> GetOrderNumberByPaymentId(int paymentId);
        Task<Result<PaymentSessionResultDto>> CreatePaymentSessionAsync(CreatePaymentSessionDto sessionDto);
        Task<Result<bool>> ConfirmPaymentAndUpdateOrderAsync(ConfirmPaymentDto confirmPaymentDto);
        Task<Result<string>> FailPaymentAndUpdateOrderAsync(FailPaymentDto failPaymentDto);
        Task<Result<string>> CancelPaymentAndUpdateOrderAsync(CancelPaymentDto cancelPaymentDto);
        Task SendOrderConfirmationEmailAsync(int orderId);
    }
}
