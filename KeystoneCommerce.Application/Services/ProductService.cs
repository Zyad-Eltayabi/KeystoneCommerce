using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Shared.Constants;

namespace KeystoneCommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IApplicationValidator<CreateProductDto> _validationService;
        private readonly IProductRepository _productRepository;
        private readonly IImageService _imageService;
        private readonly IMappingService _mappingService;

        public ProductService(IApplicationValidator<CreateProductDto> validationService, IProductRepository productRepository, IImageService imageService, IMappingService mappingService)
        {
            _validationService = validationService;
            _productRepository = productRepository;
            _imageService = imageService;
            _mappingService = mappingService;
        }

        #region Create New Product
        public async Task<Result<bool>> CreateProduct(CreateProductDto createProductDto)
        {
            var validationResult = _validationService.Validate(createProductDto);
            if (!validationResult.IsValid)
                return Result<bool>.Failure(validationResult.Errors);

            // Check if the product name is unique
            if (await _productRepository.ExistsAsync(x => x.Title == createProductDto.Title))
                return Result<bool>.Failure("The product you try add it is already exist.");

            Product product = await BuildProductFromDtoAsync(createProductDto);
            await _productRepository.AddAsync(product);
            var result = await _productRepository.SaveChangesAsync();
            return result > 0 ? Result<bool>.Success() : Result<bool>.Failure("Failed to create product.");
        }

        private async Task<Product> BuildProductFromDtoAsync(CreateProductDto createProductDto)
        {
            Product product = _mappingService.Map<Product>(createProductDto);

            product.ImageName = await _imageService.SaveImageAsync(createProductDto.MainImage.Data, createProductDto.MainImage.Type, FilePaths.ProductPath);

            var gallariesImagesName = createProductDto.Gallaries.Select(async g =>
            await _imageService.SaveImageAsync(g.Data, g.Type, FilePaths.ProductPath));

            product.Galleries = gallariesImagesName.Select(galleryImageNameTask => new ProductGallery
            {
                ImageName = galleryImageNameTask.Result
            }).ToList();
            return product;
        }

        #endregion

        public async Task<List<ProductDto>> GetAllProducts()
        {
            var products = await _productRepository.GetAllAsync();

            if (products == null || !products.Any())
                return new List<ProductDto>();

            return _mappingService.Map<List<ProductDto>>(products);
        }
    }
}
