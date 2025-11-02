using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.ProductDetails;
using KeystoneCommerce.WebUI.ViewModels.Products;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class ProductDetailsController(IProductService productService,IMappingService mappingService) : Controller
{

    [HttpGet]
    [Route("ProductDetails/{id:int}")]
    public async Task<IActionResult> Index([FromRoute] int id)
    {
        var productDto =await productService.GetProductByIdAsync(id);
        if (productDto is null)
            return View(productDto);
        var productDetailsViewModel = CreateProductDetailsViewModel(productDto);
        return View("Index", productDetailsViewModel);
    }

    private ProductDetailsViewModel CreateProductDetailsViewModel(ProductDto productDto)
    {
        var productViewModel = mappingService.Map<ProductViewModel>(productDto);
        var productDetailsViewModel = new ProductDetailsViewModel
        {
            Product = productViewModel
        };
        return productDetailsViewModel;
    }
}