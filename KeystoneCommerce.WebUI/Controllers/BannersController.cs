global using AutoMapper;
global using KeystoneCommerce.Application.DTOs;
global using KeystoneCommerce.Application.Interfaces.Services;
global using KeystoneCommerce.Shared.Constants;
global using KeystoneCommerce.WebUI.Helpers;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Rendering;
using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.WebUI.ViewModels.Banner;

namespace KeystoneCommerce.WebUI.Controllers;

[Route("Admin/[controller]")]
public class BannersController : Controller
{
    private readonly IBannerService _bannerService;
    private readonly IMapper _mapper;

    public BannersController(IBannerService bannerService, IMapper mapper)
    {
        this._bannerService = bannerService;
        _mapper = mapper;
    }

    [HttpGet]
    //[Route("Index")]
    public async Task<IActionResult> Index()
    {
        var bannersDto = await _bannerService.GetBanners();
        var bannersViewModel = _mapper.Map<List<BannerViewModel>>(bannersDto);
        return View("Index", bannersViewModel);
    }

    [HttpGet]
    [Route("Create")]
    public IActionResult Create()
    {
        var model = PrepareCreateBannerViewModel();
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Create")]
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

    [HttpGet]
    [Route("Details/{id}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var bannerDto = await _bannerService.GetById(id);
        if (bannerDto is null)
            return NotFound();
        var bannerViewModel = _mapper.Map<BannerViewModel>(bannerDto);
        return View("Details", bannerViewModel);
    }

    [HttpGet]
    [Route("Update/{id:int}")]
    public async Task<IActionResult> Update(int id)
    {
        var bannerDto = await _bannerService.GetById(id);
        if (bannerDto is null)
            return NotFound();
        var updateBannerViewModel = _mapper.Map<UpdateBannerViewModel>(bannerDto);
        updateBannerViewModel.BannerTypeNames = GetBannerTypeSelectList();
        return View("Update", updateBannerViewModel);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("Update/{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromForm] UpdateBannerViewModel model)
    {
        if (id != model.Id)
            return BadRequest();
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

    [HttpDelete]
    [Route("Delete/{id}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _bannerService.DeleteBannerAsync(id, FilePaths.BannerPath);
        return !result.IsSuccess ? NotFound(result.Errors) : Ok("Banner was deleted successfully");
    }

    private UpdateBannerDto PrepareUpdateBannerDto(UpdateBannerViewModel model)
    {
        var updateBannerDto = _mapper.Map<UpdateBannerDto>(model);
        if (model.HasNewImage)
        {
            updateBannerDto.Image = FileHelper.ConvertIFormFileToByteArray(model.Image);
            updateBannerDto.ImageUrl = FilePaths.BannerPath;
            updateBannerDto.ImageType = FileHelper.GetImageFileExtension(model.Image);
        }

        return updateBannerDto;
    }

    private CreateBannerDto PrepareCreateBannerDto(CreateBannerViewModel model)
    {
        var createBannerDto = _mapper.Map<CreateBannerDto>(model);
        createBannerDto.Image = FileHelper.ConvertIFormFileToByteArray(model.Image);
        createBannerDto.ImageUrl = FilePaths.BannerPath;
        createBannerDto.ImageType = FileHelper.GetImageFileExtension(model.Image);
        return createBannerDto;
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
}