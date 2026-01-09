using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.Interfaces.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetProductByIdAsync(int productId);
        Task<bool> AreAllProductIdsExistAsync(List<int> productIds);
        Task<List<ProductDetailsForOrderCreationDto>> GetProductsForOrderCreationAsync(List<int> productIds);
        Task DecreaseProductStock(int productId, int quantityNumberToDecrease);
        Task<List<ProductCardDto>> GetTopNewArrivalsAsync();
    }
}
