namespace KeystoneCommerce.WebUI.ViewModels.Review
{
    public class CreateReviewRequest
    {
        public int ProductId { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
