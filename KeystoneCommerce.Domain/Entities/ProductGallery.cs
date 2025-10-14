namespace KeystoneCommerce.Domain.Entities
{
    public class ProductGallery
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string ImageName { get; set; }
        public Product Product { get; set; } = null!;
    }
}
