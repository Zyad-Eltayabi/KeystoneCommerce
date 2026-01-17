using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.DTOs.ProductDetails;

public class ProductDetailsDto : BaseProductDto
{
    public int Id { get; set; }
    public string ImageName { get; set; } = null!;
    public List<string>? GalleryImageNames { get; set; } = new();
    public int TotalReviews { get; set; }
    public List<ProductCardDto>? NewArrivals { get; set; } = null!;
}