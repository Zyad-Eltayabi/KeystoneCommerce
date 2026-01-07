using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Payment;

namespace KeystoneCommerce.WebUI.ViewModels.Payment;

public class PaymentDashboardViewModel
{
    public PaymentPaginatedResult<PaymentDto> PaginatedPayments { get; set; } = null!;
    public PaymentAnalyticsViewModel TodayAnalytics { get; set; } = null!;
    public PaymentAnalyticsViewModel Last7DaysAnalytics { get; set; } = null!;
    public PaymentAnalyticsViewModel Last30DaysAnalytics { get; set; } = null!;
}
