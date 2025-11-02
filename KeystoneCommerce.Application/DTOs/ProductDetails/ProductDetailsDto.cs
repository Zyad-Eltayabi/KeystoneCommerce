using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.DTOs.ProductDetails;

public class ProductDetailsDto
{
    public ProductDto Product { get; set; } = null!;
    public List<ProductCardDto>? NewArrivals { get; set; } = null!;
}