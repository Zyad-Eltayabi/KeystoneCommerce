using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.ViewModels.Cart;

namespace KeystoneCommerce.WebUI.Services;

public class CartService(IProductService productService, CartCookieService cartCookieService)
{
    public async Task<List<ProductCartViewModel>?> GetProductCartViewModels()   
    {
        var cartItems = cartCookieService.GetCartItemsFromCookie();
        if (!cartItems.Any())
            return null;

        var cartItemProductIds = cartItems.Select(x => x.ProductId).ToList();
        var products = await productService.GetAllProducts(x => cartItemProductIds.Contains(x.Id));

        return MapProductsToProductCartViewModels(cartItems, products);
    }

    private static List<ProductCartViewModel> MapProductsToProductCartViewModels(
        List<CartViewModel> cartItems,
        List<ProductCardDto> products)
    {
        return products.Select(item =>
        {
            var count = cartItems.Single(x => x.ProductId == item.Id).Count;
            return new ProductCartViewModel
            {
                Id = item.Id,
                ImageName = item.ImageName!,
                Price = item.Price - (item.Discount ?? 0),
                Title = item.Title,
                Count = count,
                RowSumPrice = (item.Price - (item.Discount ?? 0)) * count
            };
        }).ToList();
    }
}
