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
