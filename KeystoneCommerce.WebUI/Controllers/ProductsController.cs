using AutoMapper;
using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Helpers;
using KeystoneCommerce.WebUI.ViewModels;
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
            List<ProductViewModel> productsViewModel =
                _mapper.Map<List<ProductViewModel>>(productsDto);
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
            var createProductDto = _mapper.Map<CreateProductDto>(model);
            createProductDto.MainImage = new Application.DTOs.Common.ImageDto
            {
                Data = FileHelper.ConvertIFormFileToByteArray(model.MainImage),
                Type = FileHelper.GetImageFileExtension(model.MainImage)
            };
            createProductDto.Gallaries = model.Gallaries.Select(file =>
                new Application.DTOs.Common.ImageDto
                {
                    Data = FileHelper.ConvertIFormFileToByteArray(file),
                    Type = FileHelper.GetImageFileExtension(file)
                }).ToList();
            return createProductDto;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CreateProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var createProductDto = PrepareCreateProductDto(model);
            var result = await _productService.CreateProduct(createProductDto);
            if (!result.IsSuccess)
            {
                result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
                return View("Create", model);
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region Edit Product

        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto == null)
            {
                return NotFound();
            }

            var editProductViewModel = _mapper.Map<EditProductViewModel>(productDto);
            return View(editProductViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(EditProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var editProductDto = PrepareEditProductDto(model);
                var result = await _productService.UpdateProduct(editProductDto);
                if (result.IsSuccess)
                    return RedirectToAction("Index");
                result.Errors.ForEach(error => ModelState.AddModelError(string.Empty, error));
            }

            model.GallaryImageNames = (await _productService.GetProductByIdAsync(model.Id))
                ?.GalleryImageNames;
            return View(model);
        }

        private UpdateProductDto PrepareEditProductDto(EditProductViewModel model)
        {
            var editProductDto = _mapper.Map<UpdateProductDto>(model);
            if (model.HasNewMainImage)
            {
                editProductDto.MainImage = new Application.DTOs.Common.ImageDto
                {
                    Data = FileHelper.ConvertIFormFileToByteArray(model.MainImage!),
                    Type = FileHelper.GetImageFileExtension(model.MainImage!)
                };
            }

            if (model.HasNewGallaries)
            {
                editProductDto.NewGalleries = model.Galleries?.Select(file =>
                    new Application.DTOs.Common.ImageDto
                    {
                        Data = FileHelper.ConvertIFormFileToByteArray(file),
                        Type = FileHelper.GetImageFileExtension(file)
                    }).ToList() ?? new List<Application.DTOs.Common.ImageDto>();
            }

            return editProductDto;
        }

        #endregion


        [HttpGet]
        [Route("Details/{id}")]
        public async Task<IActionResult> Details([FromRoute] int id)
        {
            var productDto = await _productService.GetProductByIdAsync(id);
            if (productDto is null)
                return NotFound();
            var productViewModel = _mapper.Map<ProductViewModel>(productDto);
            return View("Details", productViewModel);
        }

        [HttpDelete]
        [Route("Delete/{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var result = await _productService.DeleteProduct(id);
            if (!result.IsSuccess)
                return BadRequest(result.Errors);
            return Ok("The product was deleted successfully");
        }

        [HttpGet("Test")]
        public async Task<IActionResult> Test([FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var parameters = new PaginationParameters
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var paginatedProducts = await _productService.GetAllProductsPaginatedAsync(parameters);

            var viewModel = new PaginatedViewModel<ProductViewModel>
            {
                Items = _mapper.Map<List<ProductViewModel>>(paginatedProducts.Items),
                PageNumber = paginatedProducts.PageNumber,
                PageSize = paginatedProducts.PageSize,
                TotalPages = paginatedProducts.TotalPages,
                TotalCount = paginatedProducts.TotalCount
            };

            return View("Test",viewModel);
        }
    }
}