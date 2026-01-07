using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Payment;

namespace KeystoneCommerce.Application.Interfaces.Repositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<int?> GetOrderIdByPaymentIdAsync(int paymentId);
        Task<bool> IsPaymentFulfilledAsync(int paymentId);
        Task<List<Payment>> GetPaymentsPagedAsync(PaymentPaginationParameters parameters);
        Task<Payment?> GetPaymentDetailsByIdAsync(int paymentId);
        Task<PaymentAnalyticsDto> GetTodayAnalyticsAsync();
        Task<PaymentAnalyticsDto> GetLast7DaysAnalyticsAsync();
        Task<PaymentAnalyticsDto> GetLast30DaysAnalyticsAsync();
    }
}
