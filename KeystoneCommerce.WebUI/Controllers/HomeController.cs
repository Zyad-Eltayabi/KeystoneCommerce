using KeystoneCommerce.Application.DTOs.Home;
using KeystoneCommerce.WebUI.ViewModels;
using System.Diagnostics;

namespace KeystoneCommerce.WebUI.Controllers;

public class HomeController(IHomeService homeService) : Controller
{
    private readonly IHomeService _homeService = homeService;

    public async Task<IActionResult> Index()
    {
        HomePageDto model = await _homeService.GetHomePageDataAsync();
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