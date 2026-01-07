using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Application.DTOs.Payment;

public class PaymentDetailsDto
{
    public int Id { get; set; }
    public string Provider { get; set; } = null!;
    public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool IsFulfilled { get; set; }
    public string UserId { get; set; } = null!;
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public UserBasicInfoDto User { get; set; } = null!;
    public OrderDto Order { get; set; } = null!;
}
