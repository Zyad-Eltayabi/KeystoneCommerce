namespace KeystoneCommerce.Application.DTOs.Payment
{
    public class FailPaymentDto
    {
        public int PaymentId { get; set; }
        public string ProviderTransactionId { get; set; } = null!;
    }
}
