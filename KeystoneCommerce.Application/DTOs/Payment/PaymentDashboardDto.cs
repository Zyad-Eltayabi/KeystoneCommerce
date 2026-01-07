namespace KeystoneCommerce.Application.DTOs.Payment;

public class PaymentDashboardDto
{
    public PaymentPaginatedResult<PaymentDto> PaginatedPayments { get; set; } = null!;
    public PaymentAnalyticsDto TodayAnalytics { get; set; } = null!;
    public PaymentAnalyticsDto Last7DaysAnalytics { get; set; } = null!;
    public PaymentAnalyticsDto Last30DaysAnalytics { get; set; } = null!;
}
