using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Application.DTOs.Payment
{
    public class CreatePaymentDto
    {
        public PaymentType Provider { get; set; }
        public string? ProviderTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public PaymentStatus Status { get; set; }
        public string UserId { get; set; } = null!;
        public int OrderId { get; set; }
    }
}
