using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.WebUI.Services;
using KeystoneCommerce.WebUI.ViewModels.Cart;
using Microsoft.AspNetCore.Mvc;

namespace KeystoneCommerce.WebUI.Controllers
{

    public class CartController : Controller
    {
        private readonly IProductService _productService;
        private readonly CartCookieService _cartCookieService;

        public CartController(IProductService productService, CartCookieService cartCookieService)
        {
            _productService = productService;
            _cartCookieService = cartCookieService;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSmallCart([FromBody] CartViewModel request)
        {
            var product = await _productService.GetProductByIdAsync(request.ProductId);
            if (product == null)
                return NotFound();
            int cartItemsCount = _cartCookieService.UpdateCart(request);
            return Ok(cartItemsCount);
        }

        [HttpGet]
        public async Task<IActionResult> SmallCart()
        {
            List<ProductCartViewModel>? productCartItems = await GetProductCartViewModels();
            return PartialView("_SmallCartPartialView", productCartItems);
        }

        private async Task<List<ProductCartViewModel>?> GetProductCartViewModels()
        {
            var cartItems = _cartCookieService.GetCartItemsFromCookie();
            if (!cartItems.Any())
                return null;

            var cartItemProductIds = cartItems.Select(x => x.ProductId).ToList();
            var products = await _productService.GetAllProducts(x => cartItemProductIds.Contains(x.Id));
            return MapProductsToProductCartViewModels(cartItems, products);
        }

        private static List<ProductCartViewModel> MapProductsToProductCartViewModels(List<CartViewModel> cartItems, List<Application.DTOs.Shop.ProductCardDto> products)
        {
            List<ProductCartViewModel> productCartViewModels = new();
            foreach (var item in products)
            {
                var newItem = new ProductCartViewModel
                {
                    Id = item.Id,
                    ImageName = item.ImageName!,
                    Price = item.Price - (item.Discount ?? 0),
                    Title = item.Title,
                    Count = cartItems.Single(x => x.ProductId == item.Id).Count,
                    RowSumPrice = (item.Price - (item.Discount ?? 0)) * cartItems.Single(x => x.ProductId == item.Id).Count,
                };
                productCartViewModels.Add(newItem);
            }

            return productCartViewModels;
        }
    }
}
