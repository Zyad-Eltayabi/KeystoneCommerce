namespace KeystoneCommerce.Application.DTOs.Product
{
    public class BaseProductDto
    {
        public string Title { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public int QTY { get; set; }
        public string? Tags { get; set; }
    }
}
