using KeystoneCommerce.WebUI.ViewModels.Banner;

namespace KeystoneCommerce.WebUI.ViewModels.Home;

public class HomeBannersViewModel
{
    public required List<BannerViewModel> HomePage { get; set; }
    public required List<BannerViewModel> Featured { get; set; }
    public required List<BannerViewModel> TopProducts { get; set; }
}
