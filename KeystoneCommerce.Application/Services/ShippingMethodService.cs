using KeystoneCommerce.Application.DTOs.ShippingMethod;
using System.Linq.Expressions;

namespace KeystoneCommerce.Application.Services
{
    public class ShippingMethodService(IShippingMethodRepository repository, IMappingService mapping) : IShippingMethodService
    {
        public async Task<List<ShippingMethodDto>> GetAllShippingMethodsAsync()
        {
            var shippingMethods = await repository.GetAllAsync();
            return !shippingMethods.Any() ? new() : mapping.Map<List<ShippingMethodDto>>(shippingMethods);
        }

        public async Task<ShippingMethodDto?> GetShippingMethodByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            var trimmedName = name.Trim();
            var shippingMethod = await repository.FindAsync(sm =>
                sm.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
            return shippingMethod is not null ? mapping.Map<ShippingMethodDto>(shippingMethod) : null;
        }
    }
}