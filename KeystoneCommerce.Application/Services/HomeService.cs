using KeystoneCommerce.Application.DTOs.Home;

namespace KeystoneCommerce.Application.Services;

public class HomeService : IHomeService
{
    private readonly IBannerService _bannerService;
    private readonly ILogger<HomeService> _logger;
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;

    public HomeService(IBannerService bannerService, ILogger<HomeService> logger, IProductRepository productRepository, ICacheService cacheService)
    {
        _bannerService = bannerService;
        _logger = logger;
        _productRepository = productRepository;
        _cacheService = cacheService;
    }

    public async Task<HomePageDto> GetHomePageDataAsync()
    {
        const string cacheKey = "HomePage:Data";

        // Try to get cached data
        var cachedData = _cacheService.Get<HomePageDto>(cacheKey);
        if (cachedData is not null)
        {
            _logger.LogInformation("Home page data retrieved from cache");
            return cachedData;
        }

        _logger.LogInformation("Retrieving home page data from database");

        var homePageDto = new HomePageDto
        {
            bannersDto = await _bannerService.PrepareBannersForHomePage(),
            NewArrivals = await _productRepository.GetTopNewArrivalsAsync(),
            TopSellingProducts = await _productRepository.GetTopSellingProductsAsync()
        };

        _logger.LogInformation(
            "Home page data retrieved successfully. HomePageBanners: {HomePageBannersCount}, FeaturedBanners: {FeaturedBannersCount}, TopProductsBanners: {TopProductsBannersCount}",
            homePageDto.bannersDto.HomePage.Count,
            homePageDto.bannersDto.Featured.Count,
            homePageDto.bannersDto.TopProducts.Count);

        _cacheService.Set(cacheKey, homePageDto, TimeSpan.FromMinutes(10));
        _logger.LogInformation("Home page data cached successfully with 10 minute expiration");

        return homePageDto;
    }
}
