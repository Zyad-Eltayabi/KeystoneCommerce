using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.Services;

public class ShopService(IShopRepository shopRepository) : IShopService
{
    public async Task<List<ProductCardDto>> GetAvailableProducts(PaginationParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SortBy))
        {
            var splitSortByValue = parameters.SortBy.Split("-");
            if (splitSortByValue.Length == 2)
            {
                parameters.SortBy = splitSortByValue[0];
                parameters.SortOrder = splitSortByValue[1];
            }
        }
        var products = await shopRepository.GetAvailableProducts(parameters);
        return products;
    }
}