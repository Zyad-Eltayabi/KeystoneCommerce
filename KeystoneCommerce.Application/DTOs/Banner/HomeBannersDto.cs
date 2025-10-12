namespace KeystoneCommerce.Application.DTOs.Banner;

public class HomeBannersDto
{
    public required List<BannerDto> HomePage { get; set; }
    public required List<BannerDto> Featured { get; set; }
    public required List<BannerDto> TopProducts { get; set; }
}