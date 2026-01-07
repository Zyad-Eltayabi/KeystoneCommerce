using KeystoneCommerce.WebUI.ViewModels.Account;
using KeystoneCommerce.WebUI.ViewModels.Orders;

namespace KeystoneCommerce.WebUI.ViewModels.Payment;

public class PaymentDetailsViewModel
{
    public int Id { get; set; }
    public string Provider { get; set; } = null!;
    public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool IsFulfilled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public UserBasicViewModel User { get; set; } = null!;
    public OrderViewModel Order { get; set; } = null!;
}
