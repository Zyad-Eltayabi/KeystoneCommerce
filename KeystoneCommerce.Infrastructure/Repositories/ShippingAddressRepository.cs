namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class ShippingAddressRepository : GenericRepository<ShippingAddress>, IShippingAddressRepository
    {
        private readonly ApplicationDbContext _context;

        public ShippingAddressRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
