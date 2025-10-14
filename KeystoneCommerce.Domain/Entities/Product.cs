namespace KeystoneCommerce.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Summary { get; set; }
        public required string Description { get; set; }
        public required decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public required string ImageName { get; set; }
        public required int QTY { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public required ICollection<ProductGallery> Galleries { get; set; } = new List<ProductGallery>();
    }
}