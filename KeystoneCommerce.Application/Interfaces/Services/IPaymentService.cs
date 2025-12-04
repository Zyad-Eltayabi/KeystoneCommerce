using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Payment;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<Result<int>> CreatePaymentAsync(CreatePaymentDto createPaymentDto);
    }
}
