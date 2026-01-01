namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class InventoryReservationRepository : GenericRepository<InventoryReservation>, IInventoryReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public InventoryReservationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
