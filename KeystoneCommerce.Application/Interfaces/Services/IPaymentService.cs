using KeystoneCommerce.Application.DTOs.Payment;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<Result<int>> CreatePaymentAsync(CreatePaymentDto createPaymentDto);
        Task<Result<bool>> ConfirmPaymentAsync(ConfirmPaymentDto confirmPaymentDto);
        Task<Result<int>> FailPaymentAsync(int paymentId, string providerTransactionId);
        Task<Result<int>> CancelPaymentAsync(int paymentId, string providerTransactionId);
    }
}
