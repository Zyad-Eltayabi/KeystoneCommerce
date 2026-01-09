using KeystoneCommerce.Application.DTOs.Home;
using KeystoneCommerce.WebUI.ViewModels;
using KeystoneCommerce.WebUI.ViewModels.Home;
using KeystoneCommerce.WebUI.ViewModels.Shop;
using System.Diagnostics;

namespace KeystoneCommerce.WebUI.Controllers;

public class HomeController(IHomeService homeService, IMapper mapper) : Controller
{
    private readonly IHomeService _homeService = homeService;
    private readonly IMapper _mapper = mapper;

    public async Task<IActionResult> Index()
    {
        HomePageDto homePageDto = await _homeService.GetHomePageDataAsync();
        HomePageViewModel model = new()
        {
            HomeBanners = _mapper.Map<HomeBannersViewModel>(homePageDto.bannersDto),
            NewArrivals = _mapper.Map<List<ProductCardViewModel>>(homePageDto.NewArrivals),
            TopSellingProducts = _mapper.Map<List<ProductCardViewModel>>(homePageDto.TopSellingProducts)
        };
        return View("index", model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}