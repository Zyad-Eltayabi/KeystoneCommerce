using KeystoneCommerce.Application.Common.Pagination;

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

        public async Task<List<Payment>> GetPaymentsPagedAsync(PaymentPaginationParameters parameters)
        {
            var query = ConfigureQueryForPagination(parameters);

            if (parameters.Status.HasValue)
            {
                query = query.Where(p => (int)p.Status == parameters.Status.Value);
            }

            if (parameters.Provider.HasValue)
            {
                query = query.Where(p => (int)p.Provider == parameters.Provider.Value);
            }

            parameters.TotalCount = await query.CountAsync();

            return await query.AsNoTracking()
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();
        }

        public async Task<Payment?> GetPaymentDetailsByIdAsync(int paymentId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }
    }
}
