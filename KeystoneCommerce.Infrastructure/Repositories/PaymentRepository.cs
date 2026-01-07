using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Payment;

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

        public async Task<PaymentAnalyticsDto> GetTodayAnalyticsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            return await CalculateAnalytics(today, tomorrow);
        }

        public async Task<PaymentAnalyticsDto> GetLast7DaysAnalyticsAsync()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-7);
            var endDate = DateTime.UtcNow.Date.AddDays(1);
            return await CalculateAnalytics(startDate, endDate);
        }

        public async Task<PaymentAnalyticsDto> GetLast30DaysAnalyticsAsync()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-30);
            var endDate = DateTime.UtcNow.Date.AddDays(1);
            return await CalculateAnalytics(startDate, endDate);
        }

        private async Task<PaymentAnalyticsDto> CalculateAnalytics(DateTime startDate, DateTime endDate)
        {
            Expression<Func<Payment, bool>> dateRangeFilter = p => p.CreatedAt >= startDate && p.CreatedAt < endDate;
            
            var result = await _context.Payments
                .Where(dateRangeFilter)
                .GroupBy(_ => 1)
                .Select(g => new PaymentAnalyticsDto
                {
                    TotalPayments = g.Count(),
                    TotalRevenue = g.Where(p => p.Status == PaymentStatus.Successful).Sum(p => p.Amount),
                    SuccessfulPaymentsCount = g.Count(p => p.Status == PaymentStatus.Successful),
                    FailedPaymentsCount = g.Count(p => p.Status == PaymentStatus.Failed),
                    ProcessingPaymentsCount = g.Count(p => p.Status == PaymentStatus.Processing),
                    CancelledPaymentsCount = g.Count(p => p.Status == PaymentStatus.Canceled),
                    SuccessRate = g.Count() > 0 
                        ? Math.Round((decimal)g.Count(p => p.Status == PaymentStatus.Successful) * 100 / g.Count(), 2)
                        : 0,
                    AverageTransactionValue = g.Where(p => p.Status == PaymentStatus.Successful)
                        .Select(p => (decimal?)p.Amount)
                        .Average() ?? 0
                })
                .AsNoTracking()
                .FirstOrDefaultAsync() ?? new PaymentAnalyticsDto();

            return result;
        }
    }
}
