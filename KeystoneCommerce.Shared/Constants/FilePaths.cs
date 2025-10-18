namespace KeystoneCommerce.Shared.Constants
{
    public class FilePaths
    {
        // For SAVING files (physical file system)
        private const string ImageRoot = "wwwroot/assets/img";
        public const string BannerPath = ImageRoot + "/banners";
        public const string ProductPath = ImageRoot + "/products";

        // For RETRIEVING images (web URLs)
        public const string WebBannerPath = "/assets/img/banners";
        public const string WebProductPath = "/assets/img/products";
    }
}
