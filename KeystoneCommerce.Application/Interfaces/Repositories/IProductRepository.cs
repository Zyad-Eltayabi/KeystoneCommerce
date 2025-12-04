using KeystoneCommerce.Domain.Entities;

namespace KeystoneCommerce.Application.Interfaces.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetProductByIdAsync(int productId);
        Task<bool> AreAllProductIdsExistAsync(List<int> productIds);
    }
}
