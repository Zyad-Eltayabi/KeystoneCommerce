using KeystoneCommerce.Application.DTOs.Product;

namespace KeystoneCommerce.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                 .Include(p => p.Galleries)
                 .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<bool> AreAllProductIdsExistAsync(List<int> productIds)
        {
            var distinctIds = productIds.Distinct().ToList();
            var existingProductsCount = await _context.Products
                .Where(p => distinctIds.Contains(p.Id))
                .CountAsync();
            return existingProductsCount == distinctIds.Count;
        }

        public async Task<List<ProductDetailsForOrderCreationDto>> GetProductsForOrderCreationAsync(List<int> productIds)
        {
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new ProductDetailsForOrderCreationDto()
                {
                    Id = p.Id,
                    Price = p.Price,
                    Title = p.Title,
                    Discount = p.Discount
                })
                .ToListAsync();
            return products;
        }

        public async Task DecreaseProductStock(int productId, int quantityNumberToDecrease)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                EXEC SP_DecreaseProductQty @ProductId = {productId}, @ProductQty = {quantityNumberToDecrease};");
        }
    }
}
