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
        private readonly CartService _cartService;

        public CartController(IProductService productService, CartCookieService cartCookieService, CartService cartService)
        {
            _productService = productService;
            _cartCookieService = cartCookieService;
            _cartService = cartService;
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
            List<ProductCartViewModel>? productCartItems = await _cartService.GetProductCartViewModels();
            return PartialView("_SmallCartPartialView", productCartItems);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var productCartItems = await _cartService.GetProductCartViewModels();
            return View(productCartItems);
        }

        [HttpGet]
        public IActionResult ClearCart()
        {
            _cartCookieService.ClearCart();
            return RedirectToAction("Index", "Shop");
        }
    }
}
