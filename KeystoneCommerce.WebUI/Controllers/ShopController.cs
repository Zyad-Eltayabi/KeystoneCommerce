using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.Shop;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers;

public class ShopController(IShopService shopService,IMappingService mappingService) : Controller
{
    
    public async Task<IActionResult> Index([FromQuery]PaginationParameters parameters)
    {
        var productsDto = await shopService.GetAvailableProducts(parameters);
        var productsCardsViewModel = mappingService.Map<List<ProductCardViewModel>>(productsDto);
        var paginationResult = CreatePaginationResult(parameters, productsCardsViewModel);
        return View(paginationResult);
    }

    private static PaginatedResult<ProductCardViewModel> CreatePaginationResult(PaginationParameters parameters,
        List<ProductCardViewModel> productsCardsViewModel)
    {
        return new PaginatedResult<ProductCardViewModel>
        {
            Items = productsCardsViewModel,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = parameters.TotalCount,
            SortBy = parameters.SortBy,
            SortOrder = parameters.SortOrder,
            SearchBy = parameters.SearchBy,
            SearchValue = parameters.SearchValue
        };
    }
}