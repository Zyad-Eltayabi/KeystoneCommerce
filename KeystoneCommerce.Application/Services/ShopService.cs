using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Application.Services;

public class ShopService : IShopService
{
    private readonly IShopRepository _shopRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ShopService> _logger;

    public ShopService(IShopRepository shopRepository, ICacheService cacheService, ILogger<ShopService> logger)
    {
        _shopRepository = shopRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<ProductCardDto>> GetAvailableProducts(PaginationParameters parameters)
    {
        
        if (!string.IsNullOrWhiteSpace(parameters.SortBy))
        {
            var splitSortByValue = parameters.SortBy.Split("-");
            if (splitSortByValue.Length == 2)
            {
                parameters.SortBy = splitSortByValue[0];
                parameters.SortOrder = splitSortByValue[1];
            }
        }

        // Skip caching for search queries - they are too dynamic and user-specific
        if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
        {
            _logger.LogInformation(
                "Skipping cache for search query. SearchBy: {SearchBy}, SearchValue: {SearchValue}",
                parameters.SearchBy, parameters.SearchValue);

            var searchResults = await _shopRepository.GetAvailableProducts(parameters);
            return searchResults;
        }

        // Build cache key from pagination and sorting parameters (excluding search)
        var cacheKey = BuildCacheKey(parameters);

        var cachedProducts = _cacheService.Get<List<ProductCardDto>>(cacheKey);
        if (cachedProducts != null)
        {
            _logger.LogInformation(
                "Returning cached available products. Page: {PageNumber}, Size: {PageSize}, Count: {Count}",
                parameters.PageNumber, parameters.PageSize, cachedProducts.Count);
            return cachedProducts;
        }

        _logger.LogInformation(
            "Cache miss - Fetching available products. Page: {PageNumber}, Size: {PageSize}, Sort: {SortBy} {SortOrder}",
            parameters.PageNumber, parameters.PageSize, parameters.SortBy, parameters.SortOrder);

        var products = await _shopRepository.GetAvailableProducts(parameters);

        _cacheService.Set(cacheKey, products, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3));
        _logger.LogInformation(
            "Cached available products with sliding expiration. Page: {PageNumber}, Size: {PageSize}, Count: {Count}, TTL: 3 minutes",
            parameters.PageNumber, parameters.PageSize, products.Count);

        return products;
    }

    private string BuildCacheKey(PaginationParameters parameters)
    {
        // Cache key excludes search parameters (too dynamic) - only pagination and sorting
        var sortBy = string.IsNullOrWhiteSpace(parameters.SortBy)
            ? "default"
            : parameters.SortBy;

        var sortOrder = string.IsNullOrWhiteSpace(parameters.SortOrder)
            ? "default"
            : parameters.SortOrder;

        return $"Shop:GetAvailableProducts:{parameters.PageNumber}:{parameters.PageSize}:{sortBy}:{sortOrder}";
    }
}