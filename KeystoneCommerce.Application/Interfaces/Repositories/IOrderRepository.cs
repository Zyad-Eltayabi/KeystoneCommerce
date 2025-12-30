using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Domain.Entities;

namespace KeystoneCommerce.Application.Interfaces.Repositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task ReleaseReservedStock(int orderId);
        Task<string> GetOrderNumberByPaymentId(int paymentId);
        Task<List<Order>> GetOrdersPagedAsync(OrderPaginationParameters parameters);
    }
}
