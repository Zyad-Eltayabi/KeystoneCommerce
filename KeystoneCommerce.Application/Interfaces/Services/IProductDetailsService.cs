using KeystoneCommerce.Application.DTOs.ProductDetails;

namespace KeystoneCommerce.Application.Interfaces.Services;

public interface IProductDetailsService
{
    Task<ProductDetailsDto?> GetProductDetails(int productId);
}