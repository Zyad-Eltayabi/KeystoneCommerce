using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Application.Interfaces.Repositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task ReleaseReservedStock(int orderId);
        Task<string> GetOrderNumberByPaymentId(int paymentId);
        Task<List<Order>> GetOrdersPagedAsync(OrderPaginationParameters parameters);
        Task<Order?> GetOrderDetailsByIdAsync(int orderId);
        Task<OrderAnalyticsDto> GetMonthlyAnalyticsAsync();
        Task<OrderAnalyticsDto> GetTodayAnalyticsAsync();
    }
}
