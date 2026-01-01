using KeystoneCommerce.Application.DTOs.Banner;

namespace KeystoneCommerce.Application.Interfaces.Services;

public interface IBannerService
{
    Dictionary<int, string> GetBannerTypes();
    Task<Result<bool>> Create(CreateBannerDto createBannerDto);
    Task<List<BannerDto>> GetBanners();
    Task<BannerDto?> GetById(int id);
    Task<Result<bool>> UpdateBannerAsync(UpdateBannerDto updateBannerDto);
    Task<Result<bool>> DeleteBannerAsync(int id, string imageUrl);
    Task<HomeBannersDto> PrepareBannersForHomePage();
}
