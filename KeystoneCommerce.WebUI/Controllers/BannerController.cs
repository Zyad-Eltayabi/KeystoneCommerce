using AutoMapper;
using KeystoneCommerce.Application.DTOs;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Constants;
using KeystoneCommerce.WebUI.ViewModels.Banner;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using KeystoneCommerce.Application.DTOs.Banner;
using NuGet.Protocol;

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
    public async Task<IActionResult> Index()
    {
        var bannersDto = await _bannerService.GetBanners();
        var bannersViewModel = _mapper.Map<List<BannerViewModel>>(bannersDto);
        return View("index", bannersViewModel);
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
        createBannerDto.ImageUrl = FilePaths.BannerPath;
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

    [HttpGet]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var bannerDto = await _bannerService.GetById(id);
        if (bannerDto is null)
            return NotFound();
        var bannerViewModel = _mapper.Map<BannerViewModel>(bannerDto);
        return View("Details", bannerViewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var bannerDto = await _bannerService.GetById(id);
        if (bannerDto is null)
            return NotFound();
        var updateBannerViewModel = _mapper.Map<UpdateBannerViewModel>(bannerDto);
        updateBannerViewModel.BannerTypeNames = GetBannerTypeSelectList();
        return View("Update", updateBannerViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateBannerViewModel model)
    {
        if (ModelState.IsValid)
        {
            var updateBannerDto = PrepareUpdateBannerDto(model);
            var result = await _bannerService.UpdateBannerAsync(updateBannerDto);
            if (result.IsSuccess)
                return RedirectToAction("Index");
            result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
        }

        model.BannerTypeNames = GetBannerTypeSelectList();
        return View("Update", model);
    }

    private UpdateBannerDto PrepareUpdateBannerDto(UpdateBannerViewModel model)
    {
        var updateBannerDto = _mapper.Map<UpdateBannerDto>(model);
        if (model.HasNewImage)
        {
            updateBannerDto.Image = ConvertIFormFileToByteArray(model.Image);
            updateBannerDto.ImageUrl = FilePaths.BannerPath;
            updateBannerDto.ImageType = Path.GetExtension(model.Image.FileName);
        }

        return updateBannerDto;
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _bannerService.DeleteBannerAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Errors);
        return Ok("Banner was deleted successfully");
    }
}