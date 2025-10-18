using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IApplicationValidator<CreateProductDto> _validationService;
        private readonly IProductRepository _productRepository;
        private readonly IImageService _imageService;
        private readonly IMappingService _mappingService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IApplicationValidator<CreateProductDto> validationService, IProductRepository productRepository, IImageService imageService, IMappingService mappingService, ILogger<ProductService> logger)
        {
            _validationService = validationService;
            _productRepository = productRepository;
            _imageService = imageService;
            _mappingService = mappingService;
            _logger = logger;
        }

        #region Create New Product
        public async Task<Result<bool>> CreateProduct(CreateProductDto createProductDto)
        {
            _logger.LogInformation("Creating product: {@Product}", createProductDto);

            var validationResult = _validationService.Validate(createProductDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Product validation failed for {ProductTitle}. Errors: {ValidationErrors}",
                    createProductDto.Title, string.Join(", ", validationResult.Errors));
                return Result<bool>.Failure(validationResult.Errors);
            }


            // Check if the product name is unique
            if (await _productRepository.ExistsAsync(x => x.Title == createProductDto.Title))
            {
                _logger.LogWarning("Product creation failed - duplicate title found: {ProductTitle}", createProductDto.Title);
                return Result<bool>.Failure("The product you try add it is already exist.");
            }

            Product product = await BuildProductFromDtoAsync(createProductDto);

            await _productRepository.AddAsync(product);

            var result = await _productRepository.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation("Product created successfully: {ProductTitle} with {GalleryCount} gallery images",
                    createProductDto.Title, createProductDto.Gallaries.Count);
                return Result<bool>.Success();
            }
            _logger.LogError("Failed to save product to database: {ProductTitle}", createProductDto.Title);
            return Result<bool>.Failure("Failed to create product.");
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
            _logger.LogInformation("Fetching all products from the repository.");
            var products = await _productRepository.GetAllAsync();

            if (products == null || !products.Any())
            {
                _logger.LogWarning("No products found.");
                return new List<ProductDto>();
            }

            return _mappingService.Map<List<ProductDto>>(products);
        }
    }
}
