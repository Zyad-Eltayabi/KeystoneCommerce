using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;

namespace KeystoneCommerce.Application.Services;

public class ShopService(IShopRepository shopRepository) : IShopService
{
    public async Task<List<ProductCardDto>> GetAvailableProducts()
    {
        var products = await shopRepository.GetAvailableProducts();
        return products;
    }
}