namespace KeystoneCommerce.WebUI.ViewModels.Payment;

public class PaymentAnalyticsViewModel
{
    public int TotalPayments { get; set; }
    public decimal TotalRevenue { get; set; }
    public int SuccessfulPaymentsCount { get; set; }
    public int FailedPaymentsCount { get; set; }
    public int ProcessingPaymentsCount { get; set; }
    public int CancelledPaymentsCount { get; set; }
    public decimal SuccessRate { get; set; }
    public decimal AverageTransactionValue { get; set; }
}
