using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public PaymentType Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public bool IsFulfilled { get; set; } = false;
    public DateTime CreatedAt { get;private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string UserId { get; set; } = null!;
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
}