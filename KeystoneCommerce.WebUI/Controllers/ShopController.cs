using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.Shop;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class ShopController(IShopService shopService,IMappingService mappingService) : Controller
{
    
    public async Task<IActionResult> Index()
    {
        var productsDto = await shopService.GetAvailableProducts();
        var productsCardsViewModel = mappingService.Map<List<ProductCardViewModel>>(productsDto);
        return View(productsCardsViewModel);
    }
}