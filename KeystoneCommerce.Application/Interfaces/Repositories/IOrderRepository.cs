using KeystoneCommerce.Domain.Entities;

namespace KeystoneCommerce.Application.Interfaces.Repositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task ReleaseReservedStock(int orderId);
    }
}
