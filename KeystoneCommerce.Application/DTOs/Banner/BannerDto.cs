using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Application.DTOs.Banner;

public class BannerDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string SubTitle { get; set; }
    public required string ImageName { get; set; }
    public required string Link { get; set; }
    public required int Priority  { get; set; }
    public required string BannerType  { get; set; }
}