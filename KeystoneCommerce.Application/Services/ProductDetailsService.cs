using KeystoneCommerce.Application.DTOs.ProductDetails;

namespace KeystoneCommerce.Application.Services;

public class ProductDetailsService(
    IProductDetailsRepository productDetailsRepository,
    ILogger<ProductDetailsService> logger,
    ICacheService cacheService
   )
    : IProductDetailsService
{
    public async Task<ProductDetailsDto?> GetProductDetails(int productId)
    {
        var cacheKey = $"ProductDetails:GetById:{productId}";

        var cachedProductDetails = cacheService.Get<ProductDetailsDto>(cacheKey);
        if (cachedProductDetails is not null)
        {
            logger.LogInformation(
                "Product details retrieved from cache. ProductId: {ProductId}, Title: {Title}",
                productId,
                cachedProductDetails.Title);
            return cachedProductDetails;
        }

        logger.LogInformation("Fetching product details from database for product ID: {ProductId}", productId);
        var product = await productDetailsRepository.GetProductDetailsByIdAsync(productId);
        if (product == null)
        {
            logger.LogWarning("Product with ID: {ProductId} not found.", productId);
            return null;
        }

        cacheService.Set(cacheKey, product, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(3));
        logger.LogInformation(
            "Product details cached successfully with 10 minute absolute and 3 minute sliding expiration. ProductId: {ProductId}, Title: {Title}",
            productId,
            product.Title);

        return product;
    }
}