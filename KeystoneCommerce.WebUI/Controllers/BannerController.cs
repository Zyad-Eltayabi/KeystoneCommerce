using AutoMapper;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Constants;
using KeystoneCommerce.WebUI.ViewModels.Banner;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeystoneCommerce.WebUI.Controllers;

public class BannerController : Controller
{
    private readonly IBannerService _bannerService;
    private readonly IMapper _mapper;

    public BannerController(IBannerService bannerService, IMapper mapper)
    {
        this._bannerService = bannerService;
        _mapper = mapper;
    }

    // GET
    public IActionResult Index()
    {
        return View("Index");
    }

    private List<SelectListItem> GetBannerTypeSelectList()
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBannerViewModel model)
    {
        if (ModelState.IsValid)
        {
            var createBannerDto = PrepareCreateBannerDto(model);
            var result = await _bannerService.Create(createBannerDto);
            if (result.IsSuccess)
                return RedirectToAction("Index");
            result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
        }
        model.BannerTypeNames = GetBannerTypeSelectList();
        return View("Create", model);
    }

    private CreateBannerDto PrepareCreateBannerDto(CreateBannerViewModel model)
    {
        var createBannerDto = _mapper.Map<CreateBannerDto>(model);
        createBannerDto.Image = ConvertIFormFileToByteArray(model.Image);
        createBannerDto.ImageUrl = FilePaths.bannerPath;
        createBannerDto.ImageType = Path.GetExtension(model.Image.FileName);
        return createBannerDto;
    }

    private byte[] ConvertIFormFileToByteArray(IFormFile file)
    {
        using (var memoryStream = new MemoryStream())
        {
            file.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}