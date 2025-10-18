namespace KeystoneCommerce.Application.DTOs.Product
{
    public class ProductDto : BaseProductDto
    {
        public int Id { get; set; }
        public string ImageName { get; set; } = null!;
        public List<string>? GalleryImageNames { get; set; } = new();
    }
}
