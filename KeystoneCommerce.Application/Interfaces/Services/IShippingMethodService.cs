using KeystoneCommerce.Application.DTOs.ShippingMethod;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IShippingMethodService
    {
        Task<List<ShippingMethodDto>?> GetAllShippingMethodsAsync();
    }
}