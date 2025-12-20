using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int?> GetOrderIdByPaymentIdAsync(int paymentId)
        {
            return await _context.Payments
                .Where(p => p.Id == paymentId)
                .Select(p => p.OrderId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsPaymentFulfilledAsync(int paymentId)
        {
            return await _context.Payments
                .Where(p => p.Id == paymentId)
                .Select(p => p.IsFulfilled)
                .FirstOrDefaultAsync();
        }
    }
}
