using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Product;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<Result<bool>> CreateProduct(CreateProductDto createProductDto);
        Task<List<ProductDto>> GetAllProducts();
        Task<ProductDto?> GetProductByIdAsync(int productId);
        Task<Result<UpdateProductDto>> UpdateProduct(UpdateProductDto editProductDto);
        Task<Result<bool>> DeleteProduct(int id);

        Task<PaginatedResult<ProductDto>> GetAllProductsPaginatedAsync(
            PaginationParameters parameters);
    }
}
