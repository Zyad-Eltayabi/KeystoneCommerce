using KeystoneCommerce.Application.Common.Result_Pattern;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IInventoryReservationService
    {
        Task<Result<string>> CreateReservationAsync(int orderId);
    }
}
