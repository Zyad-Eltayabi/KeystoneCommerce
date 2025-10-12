using System.Diagnostics;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IBannerService _bannerService;

    public HomeController(ILogger<HomeController> logger, IBannerService bannerService)
    {
        _logger = logger;
        _bannerService = bannerService;
    }

    public async Task<IActionResult> Index()
    {
        var homeBanners = await _bannerService.PrepareBannersForHomePage();
        return View("index", homeBanners);
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