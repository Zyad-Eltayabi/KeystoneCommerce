using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.Shop;
using KeystoneCommerce.Shared.Constants;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace KeystoneCommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IApplicationValidator<CreateProductDto> _createValidationService;
        private readonly IApplicationValidator<UpdateProductDto> _updateValidationService;
        private readonly IProductRepository _productRepository;
        private readonly IImageService _imageService;
        private readonly IMappingService _mappingService;
        private readonly ILogger<ProductService> _logger;
        private readonly ICacheService _cacheService;

        public ProductService(IApplicationValidator<CreateProductDto> validationService,
            IProductRepository productRepository, IImageService imageService,
            IMappingService mappingService, ILogger<ProductService> logger,
            IApplicationValidator<UpdateProductDto> updateValidationService,
            ICacheService cacheService)
        {
            _createValidationService = validationService;
            _productRepository = productRepository;
            _imageService = imageService;
            _mappingService = mappingService;
            _logger = logger;
            _updateValidationService = updateValidationService;
            _cacheService = cacheService;
        }

        #region Create New Product

        public async Task<Result<bool>> CreateProduct(CreateProductDto createProductDto)
        {
            _logger.LogInformation("Creating product: {@Product}", createProductDto);

            var validationResult = _createValidationService.Validate(createProductDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Product validation failed for {ProductTitle}. Errors: {ValidationErrors}",
                    createProductDto.Title, string.Join(", ", validationResult.Errors));
                return Result<bool>.Failure(validationResult.Errors);
            }


            // Check if the product name is unique
            if (await _productRepository.ExistsAsync(x => x.Title == createProductDto.Title))
            {
                _logger.LogWarning(
                    "Product creation failed - duplicate title found: {ProductTitle}",
                    createProductDto.Title);
                return Result<bool>.Failure("The product you try add it is already exist.");
            }

            Product product = await BuildProductFromDtoAsync(createProductDto);

            await _productRepository.AddAsync(product);

            var result = await _productRepository.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation(
                    "Product created successfully: {ProductTitle} with {GalleryCount} gallery images",
                    createProductDto.Title, createProductDto.Gallaries.Count);

                // Invalidate relevant caches
                InvalidatePaginatedProductsCache();
                InvalidateHomePageCache();

                return Result<bool>.Success();
            }

            _logger.LogError("Failed to save product to database: {ProductTitle}",
                createProductDto.Title);
            return Result<bool>.Failure("Failed to create product.");
        }

        private async Task<Product> BuildProductFromDtoAsync(CreateProductDto createProductDto)
        {
            Product product = _mappingService.Map<Product>(createProductDto);

            product.ImageName = await _imageService.SaveImageAsync(createProductDto.MainImage.Data,
                createProductDto.MainImage.Type, FilePaths.ProductPath);

            var gallariesImagesName = createProductDto.Gallaries.Select(async g =>
                await _imageService.SaveImageAsync(g.Data, g.Type, FilePaths.ProductPath));

            product.Galleries = gallariesImagesName.Select(galleryImageNameTask =>
                new ProductGallery
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

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            var cacheKey = $"Products:GetById:{productId}";
            
            // Try to get from cache
            var cachedProduct = _cacheService.Get<ProductDto>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogInformation("Returning cached product. ID: {ProductId}", productId);
                return cachedProduct;
            }

            _logger.LogInformation("Cache miss - Fetching product with ID: {ProductId}", productId);
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product is null)
            {
                _logger.LogWarning("Product not found with ID: {ProductId}", productId);
                return null;
            }

            var productDto = _mappingService.Map<ProductDto>(product);
            
            // Cache for 15 minutes (absolute expiration)
            _cacheService.Set(cacheKey, productDto, TimeSpan.FromMinutes(15));
            _logger.LogInformation("Cached product. ID: {ProductId}, TTL: 15 minutes", productId);

            return productDto;
        }

        #region Update Product

        public async Task<Result<UpdateProductDto>> UpdateProduct(UpdateProductDto productDto)
        {
            _logger.LogInformation("Updating product with ID: {ProductId}, Title: {ProductTitle}",
                productDto.Id, productDto.Title);

            var validation = _updateValidationService.Validate(productDto);
            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Product update validation failed for ID: {ProductId}. Errors: {ValidationErrors}",
                    productDto.Id, string.Join(", ", validation.Errors));
                return Result<UpdateProductDto>.Failure(validation.Errors);
            }

            Product? product = await _productRepository.GetProductByIdAsync(productDto.Id);
            if (product is null)
            {
                _logger.LogWarning("Product not found for update. ID: {ProductId}", productDto.Id);
                return Result<UpdateProductDto>.Failure("Product not found.");
            }

            // Check for title uniqueness
            if (await _productRepository.ExistsAsync(p =>
                    p.Title == productDto.Title && p.Id != productDto.Id))
            {
                _logger.LogWarning(
                    "Product update failed - duplicate title found: {ProductTitle} for different product (ID: {ProductId})",
                    productDto.Title, productDto.Id);
                return Result<UpdateProductDto>.Failure(
                    "Another product with the same title already exists.");
            }

            // validate gallery images count
            Result<UpdateProductDto> galleryValidationResult =
                ValidateGalleryImagesCount(productDto, product);
            if (!galleryValidationResult.IsSuccess)
                return galleryValidationResult;

            RemoveDeletedGalleriesFromProduct(product, productDto.DeletedImages ?? new());

            await AddNewGalleriesToProductAsync(productDto, product);

            var oldImageName = product.ImageName;
            if (productDto.MainImage != null && !string.IsNullOrEmpty(productDto.MainImage.Type))
            {
                product.ImageName = await _imageService.SaveImageAsync(productDto.MainImage.Data,
                    productDto.MainImage.Type, FilePaths.ProductPath);
            }

            _mappingService.Map(productDto, product);
            _productRepository.Update(product);
            var result = await _productRepository.SaveChangesAsync();

            if (result == 0)
            {
                _logger.LogError(
                    "Failed to save product update to database. Product ID: {ProductId}",
                    productDto.Id);
                return Result<UpdateProductDto>.Failure("Failed to update product.");
            }

            await DeleteOldImages(product, productDto, oldImageName);

            _logger.LogInformation(
                "Product updated successfully: ID {ProductId}, Title: {ProductTitle}, Deleted Images: {DeletedCount}, New Images: {NewCount}",
                productDto.Id, productDto.Title, productDto.DeletedImages?.Count ?? 0,
                productDto.NewGalleries?.Count ?? 0);

            // Invalidate all related caches
            InvalidatePaginatedProductsCache();
            InvalidateProductByIdCache(productDto.Id);
            InvalidateHomePageCache();
            InvalidateProductDetailsCache(productDto.Id);

            return Result<UpdateProductDto>.Success(productDto);
        }

        private async Task DeleteOldImages(Product product, UpdateProductDto productDto,
            string oldImageName)
        {
            // Delete old main image if updated
            if (oldImageName != product.ImageName)
                await DeleteImage(FilePaths.ProductPath, oldImageName);

            if (productDto.HasDeletedImages)
                // Delete removed gallery images from storage
                await DeleteImages(FilePaths.ProductPath, productDto.DeletedImages!);
        }

        private async Task DeleteImage(string path, string imageName) =>
            await _imageService.DeleteImageAsync(path, imageName);

        private async Task DeleteImages(string path, List<string> imageNames)
        {
            if (imageNames is null || imageNames.Count == 0)
                return;

            foreach (var imageName in imageNames)
                await DeleteImage(path, imageName);
        }

        private async Task AddNewGalleriesToProductAsync(UpdateProductDto productDto,
            Product product)
        {
            if (!productDto.HasNewGalleries)
                return;

            foreach (var newGallery in productDto.NewGalleries!)
                product.Galleries.Add(new ProductGallery
                {
                    ImageName = await _imageService.SaveImageAsync(newGallery.Data, newGallery.Type,
                        FilePaths.ProductPath)
                });
        }

        private static void RemoveDeletedGalleriesFromProduct(Product product,
            List<string> DeletedImages)
        {
            if (DeletedImages is null || DeletedImages.Count == 0)
                return;

            DeletedImages?.ForEach(imageName =>
            {
                var deletedGalary =
                    product.Galleries
                        .Where(g => g.ImageName == imageName)
                        .FirstOrDefault();

                if (deletedGalary != null)
                    product.Galleries.Remove(deletedGalary);
            });
        }

        private Result<UpdateProductDto> ValidateGalleryImagesCount(UpdateProductDto productDto,
            Product product)
        {
            // If nothing to change, it's valid
            if (!productDto.HasDeletedImages && !productDto.HasNewGalleries)
                return Result<UpdateProductDto>.Success();

            const int max = FileSizes.MaxNumberOfGalleryImages;
            // Current number of gallery images in the product
            int existingCount = product.Galleries?.Count ?? 0;
            // Number of new gallery images to be added
            int newCount = productDto.NewGalleries?.Count ?? 0;
            // Number of gallery images to be deleted
            int deletedCount = productDto.DeletedImages?.Count ?? 0;

            // Case : trying to delete all existing images
            if (productDto.HasDeletedImages && deletedCount == existingCount)
            {
                if (!(productDto.HasNewGalleries && newCount <= max))
                {
                    _logger.LogWarning(
                        "Product update failed for ID: {ProductId} - attempting to delete all gallery images without valid replacements",
                        productDto.Id);
                    return Result<UpdateProductDto>.Failure(
                        $"You cannot delete all gallery images without adding new ones. Maximum allowed gallery images are {FileSizes.MaxNumberOfGalleryImages}.");
                }

                // If newCount <= max then OK (replacement provided)
                return Result<UpdateProductDto>.Success();
            }

            // Otherwise, compute final count after update
            int finalCountAfterUpdate = (existingCount - deletedCount) + newCount;
            if (finalCountAfterUpdate > max)
            {
                _logger.LogWarning(
                    "Product update failed for ID: {ProductId} - gallery count ({FinalCount}) exceeds limit ({MaxLimit})",
                    productDto.Id, finalCountAfterUpdate, FileSizes.MaxNumberOfGalleryImages);
                return Result<UpdateProductDto>.Failure(
                    $"The total number of gallery images exceeds the limit of {FileSizes.MaxNumberOfGalleryImages}.");
            }

            return Result<UpdateProductDto>.Success();
        }

        #endregion

        public async Task<Result<bool>> DeleteProduct(int id)
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);

            var product = await _productRepository.GetProductByIdAsync(id);
            if (product is null)
            {
                _logger.LogWarning("Product not found for deletion. ID: {ProductId}", id);
                return Result<bool>.Failure("Product not found.");
            }

            _productRepository.Delete(product);
            var result = await _productRepository.SaveChangesAsync();

            if (result == 0)
            {
                _logger.LogError("Failed to delete product from database. Product ID: {ProductId}",
                    id);
                return Result<bool>.Failure("Failed to delete product.");
            }

            // Delete main image
            await DeleteImage(FilePaths.ProductPath, product.ImageName);

            // Delete gallery images
            if (product.Galleries is not null && product.Galleries.Any())
            {
                var galleryImageNames = product.Galleries.Select(g => g.ImageName).ToList();
                await DeleteImages(FilePaths.ProductPath, galleryImageNames);
            }

            _logger.LogInformation(
                "Product deleted successfully: ID {ProductId}, Title: {ProductTitle}, Gallery Images Count: {GalleryCount}",
                id, product.Title, product.Galleries?.Count ?? 0);

            // Invalidate all related caches
            InvalidatePaginatedProductsCache();
            InvalidateProductByIdCache(id);
            InvalidateHomePageCache();
            InvalidateProductDetailsCache(id);

            return Result<bool>.Success();
        }

        public async Task<PaginatedResult<ProductDto>> GetAllProductsPaginatedAsync(
            PaginationParameters parameters)
        {
            // Skip caching for search queries - they are too dynamic and user-specific
            if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
            {
                _logger.LogInformation(
                    "Skipping cache for search query. SearchBy: {SearchBy}, SearchValue: {SearchValue}",
                    parameters.SearchBy, parameters.SearchValue);
                
                var products = await _productRepository.GetPagedAsync(parameters);
                var productDto = _mappingService.Map<List<ProductDto>>(products);

                return new PaginatedResult<ProductDto>
                {
                    Items = productDto,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalCount = parameters.TotalCount,
                    SortBy = parameters.SortBy,
                    SortOrder = parameters.SortOrder,
                    SearchBy = parameters.SearchBy,
                    SearchValue = parameters.SearchValue
                };
            }

            // Build cache key from pagination parameters (excluding search)
            var cacheKey = BuildPaginatedCacheKey(parameters);
            
            // Try to get from cache
            var cachedResult = _cacheService.Get<PaginatedResult<ProductDto>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation(
                    "Returning cached paginated products. Page: {PageNumber}, Size: {PageSize}, Total: {TotalCount}",
                    cachedResult.PageNumber, cachedResult.PageSize, cachedResult.TotalCount);
                return cachedResult;
            }

            _logger.LogInformation(
                "Cache miss - Fetching paginated products. Page: {PageNumber}, Size: {PageSize}, Sort: {SortBy} {SortOrder}",
                parameters.PageNumber, parameters.PageSize, parameters.SortBy, parameters.SortOrder);

            var productsFromDb = await _productRepository.GetPagedAsync(parameters);

            var productDtoFromDb = _mappingService.Map<List<ProductDto>>(productsFromDb);

            var result = new PaginatedResult<ProductDto>
            {
                Items = productDtoFromDb,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalCount = parameters.TotalCount,
                SortBy = parameters.SortBy,
                SortOrder = parameters.SortOrder,
                SearchBy = parameters.SearchBy,
                SearchValue = parameters.SearchValue
            };

            // Cache with 3 minutes sliding expiration - keeps frequently accessed pages fresh
            _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3));
            _logger.LogInformation(
                "Cached paginated products with sliding expiration. Page: {PageNumber}, Size: {PageSize}, TTL: 3 minutes",
                parameters.PageNumber, parameters.PageSize);

            return result;
        }

        private string BuildPaginatedCacheKey(PaginationParameters parameters)
        {
            // Cache key excludes search parameters (too dynamic) - only pagination and sorting
            var sortBy = string.IsNullOrWhiteSpace(parameters.SortBy) 
                ? "default" 
                : parameters.SortBy;

            return $"Products:Paginated:{parameters.PageNumber}:{parameters.PageSize}:{sortBy}:{parameters.SortOrder}";
        }

        public async Task<List<ProductCardDto>> GetAllProducts(Expression<Func<Product, bool>> filter)
        {
            var products = await _productRepository.GetAllAsync(filter, false);
            return _mappingService.Map<List<ProductCardDto>>(products);
        }

        public async Task<bool> AreAllProductsExistAsync(List<int> productIds)
        {
            if (productIds == null || productIds.Count == 0)
            {
                _logger.LogWarning("Product IDs validation failed - empty or null list provided");
                return false;
            }

            _logger.LogInformation("Validating existence of {Count} product IDs", productIds.Count);

            var allExist = await _productRepository.AreAllProductIdsExistAsync(productIds);

            if (!allExist)
            {
                _logger.LogWarning("One or more product IDs do not exist in the database. Provided IDs: {ProductIds}", 
                    string.Join(", ", productIds));
            }
            else
            {
                _logger.LogInformation("All {Count} product IDs validated successfully", productIds.Distinct().Count());
            }

            return allExist;
        }

        private void InvalidateHomePageCache()
        {
            const string homePageCacheKey = "HomePage:Data";
            _cacheService.Remove(homePageCacheKey);
            _logger.LogInformation("Home page cache invalidated due to product modification");
        }

        private void InvalidateProductDetailsCache(int productId)
        {
            var productDetailsCacheKey = $"ProductDetails:GetById:{productId}";
            _cacheService.Remove(productDetailsCacheKey);
            _logger.LogInformation("Product details cache invalidated for ProductId: {ProductId}", productId);
        }

        private void InvalidateProductByIdCache(int productId)
        {
            var cacheKey = $"Products:GetById:{productId}";
            _cacheService.Remove(cacheKey);
            _logger.LogInformation("Product by ID cache invalidated for ProductId: {ProductId}", productId);
        }

        private void InvalidatePaginatedProductsCache()
        {
            // Pattern-based invalidation for all "Products:Paginated:*" keys
            _cacheService.RemoveByPrefix("Products:Paginated:");
            _logger.LogInformation("Paginated products cache invalidated (pattern-based)");
        }
    }
}