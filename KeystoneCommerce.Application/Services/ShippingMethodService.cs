using KeystoneCommerce.Application.DTOs.ShippingMethod;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace KeystoneCommerce.Application.Services
{
    public class ShippingMethodService(IShippingMethodRepository repository,IMappingService mapping) : IShippingMethodService
    {
        public async Task<List<ShippingMethodDto>?> GetAllShippingMethodsAsync()
        {
            var shippingMethods = await repository.GetAllAsync();
            if (shippingMethods is null || !shippingMethods.Any())
                return null;
            return mapping.Map<List<ShippingMethodDto>>(shippingMethods);
        }
    }
}