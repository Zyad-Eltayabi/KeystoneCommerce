using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.DTOs.Home;

public class HomePageDto
{
    public HomeBannersDto bannersDto { get; set; } = null!;
    public List<ProductCardDto> NewArrivals { get; set; } = null!;
    public List<ProductCardDto> TopSellingProducts { get; set; } = null!;
}