using KeystoneCommerce.Application.DTOs.ShippingDetails;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IShippingAddressService
    {
        Task<Result<int>> CreateNewAddress(CreateShippingDetailsDto createShippingDetailsDto);
    }
}
