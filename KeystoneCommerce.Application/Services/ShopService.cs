using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.Services;

public class ShopService(IShopRepository shopRepository) : IShopService
{
    public async Task<List<ProductCardDto>> GetAvailableProducts(PaginationParameters parameters)
    {
        var products = await shopRepository.GetAvailableProducts(parameters);
        return products;
    }
}