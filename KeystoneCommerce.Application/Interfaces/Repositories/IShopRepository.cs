using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Domain.Entities;

namespace KeystoneCommerce.Application.Interfaces.Repositories;

public interface IShopRepository :  IGenericRepository<Product>
{
    public Task<List<ProductCardDto>> GetAvailableProducts(PaginationParameters parameters);
}