namespace KeystoneCommerce.Application.DTOs.Banner;

public class UpdateBannerDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string Link { get; set; }
    public int Priority { get; set; }
    public int BannerType { get; set; }
    public byte[] Image { get; set; } = [];
    public string ImageUrl { get; set; }
    public string ImageType { get; set; }
}