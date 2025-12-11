namespace KeystoneCommerce.Application.DTOs.Payment
{
    public class CreatePaymentSessionDto
    {
        public List<PaymentLineItemDto> LineItems { get; set; } = new();
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }

    public class PaymentLineItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
