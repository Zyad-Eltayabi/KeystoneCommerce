using AutoMapper;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Helpers;
using KeystoneCommerce.WebUI.ViewModels.Products;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers
{
    [Route("Admin/[controller]")]
    public class ProductsController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IProductService _productService;



        public ProductsController(IMapper mapper, IProductService productService)
        {
            _mapper = mapper;
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var productsDto = await _productService.GetAllProducts();
            List<ProductViewModel> productsViewModel = _mapper.Map<List<ProductViewModel>>(productsDto);
            return View(productsViewModel);
        }

        #region Create Product
        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            return View();
        }

        private CreateProductDto PrepareCreateProductDto(CreateProductViewModel model)
        {
            var CreateProductDto = _mapper.Map<CreateProductDto>(model);
            CreateProductDto.MainImage = new Application.DTOs.Common.ImageDto
            {
                Data = FileHelper.ConvertIFormFileToByteArray(model.MainImage),
                Type = FileHelper.GetImageFileExtension(model.MainImage)
            };
            CreateProductDto.Gallaries = model.Gallaries?.Select(file => new Application.DTOs.Common.ImageDto
            {
                Data = FileHelper.ConvertIFormFileToByteArray(file),
                Type = FileHelper.GetImageFileExtension(file)
            }).ToList() ?? new List<Application.DTOs.Common.ImageDto>();
            return CreateProductDto;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CreateProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }
            var CreateProductDto = PrepareCreateProductDto(model);
            var result = await _productService.CreateProduct(CreateProductDto);
            if (!result.IsSuccess)
            {
                result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
                return View("Create", model);
            }
            return RedirectToAction("Index");
        }
        #endregion
    }
}
