using KeystoneCommerce.Domain.Entities;

namespace KeystoneCommerce.Application.Interfaces.Repositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<int?> GetOrderIdByPaymentIdAsync(int paymentId);
        Task<bool> IsPaymentFulfilledAsync(int paymentId);
    }
}
