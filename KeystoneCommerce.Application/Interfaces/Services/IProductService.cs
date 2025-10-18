using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Product;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<Result<bool>> CreateProduct(CreateProductDto createProductDto);
    }
}
