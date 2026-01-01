using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.Interfaces.Services;

public interface IShopService
{
    public Task<List<ProductCardDto>> GetAvailableProducts(PaginationParameters parameters);
}