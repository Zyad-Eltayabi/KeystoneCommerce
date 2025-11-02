using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.ProductDetails;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.ProductDetails;
using KeystoneCommerce.WebUI.ViewModels.Products;
using KeystoneCommerce.WebUI.ViewModels.Shop;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class ProductDetailsController(
    IProductDetailsService productDetailsService,
    IMappingService mappingService) : Controller
{
    [HttpGet]
    [Route("ProductDetails/{id:int}")]
    public async Task<IActionResult> Index([FromRoute] int id)
    {
        var productDetailsDto = await productDetailsService.GetProductDetails(id);
        if (productDetailsDto?.Product is null)
            return NotFound();
        var productDetailsViewModel = CreateProductDetailsViewModel(productDetailsDto);
        return View("Index", productDetailsViewModel);
    }

    private ProductDetailsViewModel CreateProductDetailsViewModel(ProductDetailsDto productDto)
    {
        var productViewModel = mappingService.Map<ProductViewModel>(productDto.Product);
        var newArrivalsViewModels = new List<ProductCardViewModel>();
        if (productDto.NewArrivals is not null)
            newArrivalsViewModels =
                mappingService.Map<List<ProductCardViewModel>>(productDto.NewArrivals);
        return new ProductDetailsViewModel
        {
            Product = productViewModel,
            NewArrivals = newArrivalsViewModels
        };
    }
}