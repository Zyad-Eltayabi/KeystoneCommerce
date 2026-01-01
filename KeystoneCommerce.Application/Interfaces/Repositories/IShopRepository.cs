using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.Interfaces.Repositories;

public interface IShopRepository :  IGenericRepository<Product>
{
    public Task<List<ProductCardDto>> GetAvailableProducts(PaginationParameters parameters);
}