using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task ReleaseReservedStock(int orderId)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC SP_ReleaseReservedStock {orderId}");
        }

        public async Task<string> GetOrderNumberByPaymentId(int paymentId)
        {
            var orderNumber = await (from o in _context.Orders
                                     join p in _context.Payments
                                     on o.Id equals p.OrderId
                                     where p.Id == paymentId
                                     select o.OrderNumber)
                              .FirstOrDefaultAsync();
            return orderNumber ?? string.Empty;
        }

        public async Task<List<Order>> GetOrdersPagedAsync(OrderPaginationParameters parameters)
        {
            var query = ConfigureQueryForPagination(parameters);

            if (parameters.Status.HasValue)
            {
                query = query.Where(o => (int)o.Status == parameters.Status.Value);
            }

            parameters.TotalCount = await query.CountAsync();

            return await query.AsNoTracking()
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();
        }

        public async Task<Order?> GetOrderDetailsByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.ShippingAddress)
                .Include(o => o.ShippingMethod)
                .Include(o => o.Payment)
                .Include(o => o.Coupon)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<OrderAnalyticsDto> GetMonthlyAnalyticsAsync()
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);
            return await CalculateAnalytics(startOfMonth, endOfMonth);
        }

        public async Task<OrderAnalyticsDto> GetTodayAnalyticsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            return await CalculateAnalytics(today, tomorrow);
        }

        private async Task<OrderAnalyticsDto> CalculateAnalytics(DateTime startDate, DateTime endDate)
        {
            Expression<Func<Order, bool>> dateRangeFilter = o => o.CreatedAt >= startDate && o.CreatedAt < endDate;
            var result = await _context.Orders
           .Where(dateRangeFilter)
           .GroupBy(_ => 1)
           .Select(g => new OrderAnalyticsDto
           {
               OrderCount = g.Count(),
               OrdersRevenue = g.Where(o => o.IsPaid).Sum(o => o.Total),
               PendingOrdersCount = g.Count(o => o.Status == OrderStatus.Processing),
               FailedOrdersCount = g.Count(o => o.Status == OrderStatus.Failed),
               CancellationOrdersCount = g.Count(o => o.Status == OrderStatus.Cancelled),
               AverageOrderValue = g.Select(o => (decimal?)o.Total).Average() ?? 0
           })
           .AsNoTracking()
           .FirstOrDefaultAsync() ?? new();
            return result;
        }
    }
}
