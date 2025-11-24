using Azure;
using KeystoneCommerce.WebUI.ViewModels.Cart;
using Newtonsoft.Json;

namespace KeystoneCommerce.WebUI.Services
{
    public class CartCookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartCookieName = "Cart";

        public CartCookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpContext HttpContext =>
            _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No HttpContext available.");

        private HttpRequest _request => HttpContext.Request;
        private HttpResponse _response => HttpContext.Response;


        public List<CartViewModel> GetCartItemsFromCookie()
        {
            string? prev = _request?.Cookies[CartCookieName];

            if (!string.IsNullOrEmpty(prev))
                return JsonConvert.DeserializeObject<List<CartViewModel>>(prev)
                       ?? new List<CartViewModel>();

            return new List<CartViewModel>();
        }

        public int UpdateCart(CartViewModel request)
        {
            List<CartViewModel> cartItems = GetUpdatedCartItems(request);
            var serializedCart = JsonConvert.SerializeObject(cartItems);
            SaveCookies(serializedCart, new CookieOptions { 
                Expires = DateTime.Now.AddDays(7) ,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            return cartItems.Sum(x => x.Count);
        }

        private List<CartViewModel> GetUpdatedCartItems(CartViewModel request)
        {
            // Retrieve the list of products in the cart using the dedicated function
            var cartItems = GetCartItemsFromCookie();

            var foundProductInCart = cartItems.FirstOrDefault(x => x.ProductId == request.ProductId);

            // If the product is found, it means it is in the cart, and the user intends to change the quantity
            if (foundProductInCart == null)
            {
                var newCartItem = new CartViewModel() { };
                newCartItem.ProductId = request.ProductId;
                newCartItem.Count = request.Count;
                cartItems.Add(newCartItem);
            }
            else
            {
                // If greater than zero, it means the user wants to update the quantity; otherwise, it will be removed from the cart.
                if (request.Count > 0)
                    foundProductInCart.Count += request.Count;
                else
                    cartItems.Remove(foundProductInCart);
            }

            return cartItems;
        }

        private void SaveCookies(string json, CookieOptions options)
        {
            _response.Cookies.Append(CartCookieName, json, options);
        }
    }
}
