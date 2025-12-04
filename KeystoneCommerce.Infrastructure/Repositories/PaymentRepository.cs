using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Data;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        
    }
}
