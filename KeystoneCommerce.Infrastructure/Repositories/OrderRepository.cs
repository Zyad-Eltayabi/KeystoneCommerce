using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
    }
}
