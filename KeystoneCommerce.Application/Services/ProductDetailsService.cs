using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.ProductDetails;
using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services;

public class ProductDetailsService(
    IProductRepository productRepository,
    IProductDetailsRepository productDetailsRepository,
    ILogger<ProductDetailsService> logger,
    IMappingService mappingService)
    : IProductDetailsService
{
    public async Task<ProductDetailsDto?> GetProductDetails(int productId)
    {
        logger.LogInformation("Fetching product details for product ID: {ProductId}", productId);
        var product = await productRepository.GetProductByIdAsync(productId);
        if (product == null)
        {
            logger.LogWarning("Product with ID: {ProductId} not found.", productId);
            return null;
        }
        var productDto = mappingService.Map<ProductDto>(product);
        var newArrivals = await productDetailsRepository.GetNewArrivalsExcludingProduct(productId);
        if (newArrivals is null)
        {
            logger.LogWarning("No new arrivals found excluding product ID: {ProductId}", productId);
            newArrivals = new List<ProductCardDto>();
        }
        return new ProductDetailsDto()
        {
            Product = productDto,
            NewArrivals = newArrivals
        };
    }
}