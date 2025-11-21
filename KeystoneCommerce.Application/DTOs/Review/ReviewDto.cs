namespace KeystoneCommerce.Application.DTOs.Review
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public string UserFullName { get; set; } = string.Empty;
    }
}