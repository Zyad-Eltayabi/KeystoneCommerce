using KeystoneCommerce.WebUI.ViewModels.ProductDetails;

namespace KeystoneCommerce.WebUI.Controllers;

public class ProductDetailsController(
    IProductDetailsService productDetailsService,
    IMapper mapper) : Controller
{
    [HttpGet]
    [Route("ProductDetails/{id:int}")]
    public async Task<IActionResult> Index([FromRoute] int id)
    {
        if (id <= 0)
            return BadRequest();
        var productDetailsDto = await productDetailsService.GetProductDetails(id);
        return productDetailsDto is null ? NotFound() : View("Index", mapper.Map<ProductDetailsViewModel>(productDetailsDto));
    }
}