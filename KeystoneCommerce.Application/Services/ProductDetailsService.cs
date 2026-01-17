using KeystoneCommerce.Application.DTOs.ProductDetails;

namespace KeystoneCommerce.Application.Services;

public class ProductDetailsService(
    IProductDetailsRepository productDetailsRepository,
    ILogger<ProductDetailsService> logger
   )
    : IProductDetailsService
{
    public async Task<ProductDetailsDto?> GetProductDetails(int productId)
    {
        logger.LogInformation("Fetching product details for product ID: {ProductId}", productId);
        var product = await productDetailsRepository.GetProductDetailsByIdAsync(productId);
        if (product == null)
        {
            logger.LogWarning("Product with ID: {ProductId} not found.", productId);
            return null;
        }
        return product;
    }
}