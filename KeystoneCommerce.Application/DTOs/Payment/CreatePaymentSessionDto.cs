namespace KeystoneCommerce.Application.DTOs.Payment
{
    public class CreatePaymentSessionDto
    {
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public int PaymentId { get; set; }
    }
}
