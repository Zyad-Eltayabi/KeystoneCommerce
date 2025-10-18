namespace KeystoneCommerce.WebUI.ViewModels.Products
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public int QTY { get; set; }
        public string? Tags { get; set; }
        public string ImageName { get; set; } = null!;
        public List<string>? GalleryImageNames { get; set; } = new();
    }
}
