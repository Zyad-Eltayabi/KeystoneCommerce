using KeystoneCommerce.Application.DTOs.Home;

namespace KeystoneCommerce.Application.Services;

public class HomeService : IHomeService
{
    private readonly IBannerService _bannerService;
    private readonly ILogger<HomeService> _logger;
    private readonly IProductRepository _productRepository;

    public HomeService(IBannerService bannerService, ILogger<HomeService> logger, IProductRepository productRepository)
    {
        _bannerService = bannerService;
        _logger = logger;
        _productRepository = productRepository;
    }

    public async Task<HomePageDto> GetHomePageDataAsync()
    {
        _logger.LogInformation("Retrieving home page data");

        var homePageDto = new HomePageDto
        {
            bannersDto = await _bannerService.PrepareBannersForHomePage(),
            NewArrivals = await _productRepository.GetTopNewArrivalsAsync(),
        };

        _logger.LogInformation(
            "Home page data retrieved successfully. HomePageBanners: {HomePageBannersCount}, FeaturedBanners: {FeaturedBannersCount}, TopProductsBanners: {TopProductsBannersCount}",
            homePageDto.bannersDto.HomePage.Count,
            homePageDto.bannersDto.Featured.Count,
            homePageDto.bannersDto.TopProducts.Count);

        return homePageDto;
    }
}
