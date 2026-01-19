using KeystoneCommerce.Application.DTOs.Review;

namespace KeystoneCommerce.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMappingService _mappingService;
        private readonly IProductRepository _productRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            IReviewRepository reviewRepository, 
            IMappingService mappingService, 
            IProductRepository productRepository,
            ICacheService cacheService,
            ILogger<ReviewService> logger)
        {
            _reviewRepository = reviewRepository;
            _mappingService = mappingService;
            _productRepository = productRepository;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<PaginatedResult<ReviewDto>?> GetProductReviews(PaginationParameters parameters)
        {
            // Extract ProductId from SearchValue 
           
            var productIdFilter = parameters.SearchBy?.Equals("ProductId", StringComparison.OrdinalIgnoreCase) == true 
                ? parameters.SearchValue ?? "all" 
                : "all";

            var cacheKey = $"Review:GetProductReviews:{productIdFilter}:{parameters.PageNumber}:{parameters.PageSize}:{parameters.SortBy}:{parameters.SortOrder}:{parameters.SearchBy}:{parameters.SearchValue}";

            var cachedReviews = _cacheService.Get<PaginatedResult<ReviewDto>>(cacheKey);
            if (cachedReviews is not null)
            {
                _logger.LogInformation(
                    "Product reviews retrieved from cache. ProductFilter: {ProductFilter}, PageNumber: {PageNumber}, PageSize: {PageSize}, TotalCount: {TotalCount}",
                    productIdFilter,
                    parameters.PageNumber,
                    parameters.PageSize,
                    cachedReviews.TotalCount);
                return cachedReviews;
            }

            _logger.LogInformation(
                "Retrieving product reviews from database. ProductFilter: {ProductFilter}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                productIdFilter,
                parameters.PageNumber,
                parameters.PageSize);

            var reviews = await _reviewRepository.GetPagedAsync(parameters);
            if (reviews is null)
            {
                _logger.LogWarning("No reviews found in database. ProductFilter: {ProductFilter}", productIdFilter);
                return null;
            }

            var result = new PaginatedResult<ReviewDto>
            {
                Items = _mappingService.Map<List<ReviewDto>>(reviews),
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalCount = parameters.TotalCount,
                SortBy = parameters.SortBy,
                SortOrder = parameters.SortOrder,
                SearchBy = parameters.SearchBy,
                SearchValue = parameters.SearchValue
            };

            _logger.LogInformation(
                "Product reviews retrieved successfully. ProductFilter: {ProductFilter}, PageNumber: {PageNumber}, PageSize: {PageSize}, TotalCount: {TotalCount}",
                productIdFilter,
                parameters.PageNumber,
                parameters.PageSize,
                result.TotalCount);

            _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(3));
            _logger.LogInformation(
                "Product reviews cached successfully with 5 minute absolute and 3 minute sliding expiration. ProductFilter: {ProductFilter}, CacheKey: {CacheKey}",
                productIdFilter,
                cacheKey);

            return result;
        }

        private async Task<Result<CreateReviewDto>> validAsync(CreateReviewDto CreateReviewDto)
        {
            if (string.IsNullOrWhiteSpace(CreateReviewDto.Comment))
                return Result<CreateReviewDto>.Failure("Comment can't be null or empty");

            if (CreateReviewDto.Comment.Length > 2500)
                return Result<CreateReviewDto>.Failure("Comment can't be longer than 2500 characters");

            if (!await _productRepository.ExistsAsync(p => p.Id == CreateReviewDto.ProductId))
                return Result<CreateReviewDto>.Failure("Product does not exist");

            return Result<CreateReviewDto>.Success();
        }

        public async Task<Result<CreateReviewDto>> CreateNewReview(CreateReviewDto createReviewDto)
        {
            _logger.LogInformation(
                "Creating new review. ProductId: {ProductId}, UserId: {UserId}",
                createReviewDto.ProductId,
                createReviewDto.UserId);

            var validationResult = await validAsync(createReviewDto);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Review creation validation failed. ProductId: {ProductId}, ValidationErrors: {@ValidationErrors}",
                    createReviewDto.ProductId,
                    validationResult.Errors);
                return validationResult;
            }

            Review review = MapCreateReviewDtoToReview(createReviewDto);
            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Review created successfully. ProductId: {ProductId}, UserId: {UserId}, UserFullName: {UserFullName}",
                createReviewDto.ProductId,
                createReviewDto.UserId,
                createReviewDto.UserFullName);

            // Invalidate review caches for the specific product
            InvalidateReviewCaches(createReviewDto.ProductId);

            return Result<CreateReviewDto>.Success(createReviewDto);
        }

        private static Review MapCreateReviewDtoToReview(CreateReviewDto CreateReviewDto)
        {
            return new Review
            {
                ProductId = CreateReviewDto.ProductId,
                UserId = CreateReviewDto.UserId,
                Comment = CreateReviewDto.Comment,
                UserFullName = CreateReviewDto.UserFullName
            };
        }

        private void InvalidateReviewCaches(int productId)
        {
            // Invalidate only caches for the specific product that received a new review
            var productReviewCachePrefix = $"Review:GetProductReviews:{productId}:";

            _cacheService.RemoveByPrefix(productReviewCachePrefix);
            _logger.LogInformation(
                "Review caches invalidated for product. ProductId: {ProductId}, CachePrefix: {CachePrefix}",
                productId,
                productReviewCachePrefix);
        }
    }
}