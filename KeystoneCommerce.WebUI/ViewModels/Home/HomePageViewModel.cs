using KeystoneCommerce.WebUI.ViewModels.Shop;

namespace KeystoneCommerce.WebUI.ViewModels.Home;

public class HomePageViewModel
{
    public HomeBannersViewModel HomeBanners { get; set; } = null!;
    public List<ProductCardViewModel> NewArrivals { get; set; } = null!;
    public List<ProductCardViewModel> TopSellingProducts { get; set; } = null!;
}
