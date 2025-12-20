using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Infrastructure.Persistence.Data;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class ShippingMethodRepository : GenericRepository<ShippingMethod>, IShippingMethodRepository
    {
        public ShippingMethodRepository(ApplicationDbContext applicationDbContext)
            : base(applicationDbContext)
        {
        }
    }
}
