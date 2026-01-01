namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class BannerRepository : GenericRepository<Banner>, IBannerRepository
    {
        private readonly ApplicationDbContext _context;
        public BannerRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
            _context = applicationDbContext;
        }

    }
}
