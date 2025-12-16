namespace KeystoneCommerce.Application.DTOs.Payment
{
    public class CancelPaymentDto
    {
        public int PaymentId { get; set; }
        public string ProviderTransactionId { get; set; } = null!;
    }
}
