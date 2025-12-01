using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Data;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class CouponRepository : GenericRepository<Coupon>, ICouponRepository
    {
        private readonly ApplicationDbContext _context;
        public CouponRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
