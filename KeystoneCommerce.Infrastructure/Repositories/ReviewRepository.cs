using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Data;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
            this._context = context;
        }
    }
}