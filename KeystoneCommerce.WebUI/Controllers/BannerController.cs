using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.Banner;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeystoneCommerce.WebUI.Controllers;

public class BannerController : Controller
{
    private readonly IBannerService _bannerService;
    public BannerController(IBannerService bannerService)
    {
        this._bannerService = bannerService;
    }

    // GET
    public IActionResult Index()
    {
        return View("Index");
    }

    private List<SelectListItem> GetBannerTypeSelectList ()
    {
        var bannerTypes = _bannerService.GetBannerTypes();
        return bannerTypes.Select(b => new SelectListItem
        {
            Value = b.Key.ToString(),
            Text = b.Value
        }).ToList();
    }

    private CreateBannerViewModel PrepareCreateBannerViewModel()
    {
        return new CreateBannerViewModel()
        {
            BannerTypeNames = GetBannerTypeSelectList()
        };
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = PrepareCreateBannerViewModel();
        return View("Create", model);
    }
}