using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.WebUI.ViewModels.Products;
using KeystoneCommerce.WebUI.ViewModels.Shop;

namespace KeystoneCommerce.WebUI.ViewModels.ProductDetails;

public class ProductDetailsViewModel : ProductViewModel
{
    public int TotalReviews { get; set; }
    public List<ProductCardViewModel>? NewArrivals { get; set; }
}