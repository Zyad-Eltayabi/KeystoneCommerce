namespace KeystoneCommerce.Application.DTOs.Payment;

public class PaymentDashboardDto
{
    public PaymentPaginatedResult<PaymentDto> PaginatedPayments { get; set; } = null!;
    public PaymentAnalyticsDto MonthlyAnalytics { get; set; } = null!;
    public PaymentAnalyticsDto TodayAnalytics { get; set; } = null!;
}
