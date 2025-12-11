namespace KeystoneCommerce.Application.DTOs.Payment
{
    public class PaymentSessionResultDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
    }
}
