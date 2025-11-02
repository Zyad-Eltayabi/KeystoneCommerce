using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.Interfaces.Repositories;

public interface IProductDetailsRepository
{
    Task<List<ProductCardDto>?> GetNewArrivalsExcludingProduct(int productId);
}