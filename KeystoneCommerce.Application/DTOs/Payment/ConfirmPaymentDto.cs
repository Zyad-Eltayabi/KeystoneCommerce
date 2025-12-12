namespace KeystoneCommerce.Application.DTOs.Payment
{
    public class ConfirmPaymentDto
    {
        public int PaymentId { get; set; }
        public string ProviderTransactionId { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
