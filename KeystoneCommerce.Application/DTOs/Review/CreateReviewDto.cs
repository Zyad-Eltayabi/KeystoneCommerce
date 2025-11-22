namespace KeystoneCommerce.Application.DTOs.Review
{
    public class CreateReviewDto
    {
        public int ProductId { get; set; } 
        public string Comment { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
    }
}