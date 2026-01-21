using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ShopServiceTests")]
public class ShopServiceTest
{
    private readonly Mock<IShopRepository> _mockShopRepository;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<ShopService>> _mockLogger;
    private readonly ShopService _sut;

    public ShopServiceTest()
    {
        _mockShopRepository = new Mock<IShopRepository>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<ShopService>>();
        _sut = new ShopService(_mockShopRepository.Object, _mockCacheService.Object, _mockLogger.Object);
    }

    #region GetAvailableProducts Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task GetAvailableProducts_ShouldReturnProducts_WhenProductsExist()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var products = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Product 1", 99.99m),
            CreateProductCardDto(2, "Product 2", 149.99m),
            CreateProductCardDto(3, "Product 3", 199.99m)
        };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(products);

        _mockShopRepository.Verify(r => r.GetAvailableProducts(parameters), Times.Once);
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<List<ProductCardDto>>(), 
            It.IsAny<TimeSpan>(), 
            It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldReturnEmptyList_WhenNoProductsExist()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldReturnProductsWithDiscounts()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var products = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Discounted Product", 100m, 20m)
        };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().HaveCount(1);
        result[0].Discount.Should().Be(20m);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldReturnProductsWithoutDiscounts()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var products = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Regular Product", 100m, null)
        };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().HaveCount(1);
        result[0].Discount.Should().BeNull();
    }

    #endregion

    #region SortBy Splitting Scenarios

    [Theory]
    [InlineData("Price-Descending", "Price", "Descending")]
    [InlineData("Price-Ascending", "Price", "Ascending")]
    [InlineData("Title-Ascending", "Title", "Ascending")]
    [InlineData("Title-Descending", "Title", "Descending")]
    [InlineData("Discount-Descending", "Discount", "Descending")]
    [InlineData("Id-Descending", "Id", "Descending")]
    public async Task GetAvailableProducts_ShouldSplitSortBy_WhenValidFormatProvided(
        string sortBy, string expectedSortBy, string expectedSortOrder)
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = sortBy
        };

        PaginationParameters? capturedParameters = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .Callback<PaginationParameters>(p => capturedParameters = p)
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SortBy.Should().Be(expectedSortBy);
        capturedParameters.SortOrder.Should().Be(expectedSortOrder);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldNotSplitSortBy_WhenNoDelimiterPresent()
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Price"
        };

        PaginationParameters? capturedParameters = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .Callback<PaginationParameters>(p => capturedParameters = p)
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SortBy.Should().Be("Price");
        capturedParameters.SortOrder.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldNotSplitSortBy_WhenMultipleDelimitersPresent()
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Price-Desc-Test"
        };

        PaginationParameters? capturedParameters = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .Callback<PaginationParameters>(p => capturedParameters = p)
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SortBy.Should().Be("Price-Desc-Test");
        capturedParameters.SortOrder.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAvailableProducts_ShouldNotSplitSortBy_WhenSortByIsNullOrWhitespace(string? sortBy)
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = sortBy
        };

        PaginationParameters? capturedParameters = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .Callback<PaginationParameters>(p => capturedParameters = p)
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SortBy.Should().Be(sortBy);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldHandleEmptyPartsAfterSplit()
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "-"
        };

        PaginationParameters? capturedParameters = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .Callback<PaginationParameters>(p => capturedParameters = p)
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SortBy.Should().Be(string.Empty);
        capturedParameters.SortOrder.Should().Be(string.Empty);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldSplitSortBy_WithLeadingTrailingDelimiters()
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Price-Descending-"
        };

        PaginationParameters? capturedParameters = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>())
)
            .Callback<PaginationParameters>(p => capturedParameters = p)
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert - Should not split since it has 3 parts
        capturedParameters.Should().NotBeNull();
        capturedParameters!.SortBy.Should().Be("Price-Descending-");
        capturedParameters.SortOrder.Should().BeNullOrEmpty();
    }

    #endregion

    #region Pagination Scenarios

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    public async Task GetAvailableProducts_ShouldHandleDifferentPageSizes(int pageNumber, int pageSize)
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = pageNumber, PageSize = pageSize };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().NotBeNull();
        _mockShopRepository.Verify(r => r.GetAvailableProducts(
            It.Is<PaginationParameters>(p => p.PageNumber == pageNumber && p.PageSize == pageSize)), Times.Once);
    }

    #endregion

    #region Repository Interaction Tests

    [Fact]
    public async Task GetAvailableProducts_ShouldCallRepository_ExactlyOnce()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        _mockShopRepository.Verify(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldReturnSameInstance_FromRepository()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var products = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Product 1", 99.99m)
        };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().BeSameAs(products);
    }

    #endregion

    #endregion

    #region Caching Tests

    #region Cache Hit Scenarios

    [Fact]
    public async Task GetAvailableProducts_ShouldReturnCachedData_WhenCacheHit()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var cachedProducts = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Cached Product 1", 99.99m),
            CreateProductCardDto(2, "Cached Product 2", 149.99m)
        };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns(cachedProducts);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(cachedProducts);
        result.Should().HaveCount(2);

        // Repository should NOT be called when cache hits
        _mockShopRepository.Verify(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()), Times.Never);
        
        // Cache Set should NOT be called when cache hits
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<List<ProductCardDto>>(), 
            It.IsAny<TimeSpan>(), 
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldUseCacheKey_WithCorrectFormat()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 2, 
            PageSize = 20, 
            SortBy = "Price-Descending"
        };

        var expectedCacheKey = "Shop:GetAvailableProducts:2:20:Price:Descending";
        string? actualCacheKey = null;

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Callback<string>(key => actualCacheKey = key)
            .Returns((List<ProductCardDto>?)null);

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        actualCacheKey.Should().Be(expectedCacheKey);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldGenerateUniqueCacheKeys_ForDifferentParameters()
    {
        // Arrange
        var parameters1 = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var parameters2 = new PaginationParameters { PageNumber = 2, PageSize = 10 };
        var parameters3 = new PaginationParameters { PageNumber = 1, PageSize = 20 };

        var capturedKeys = new List<string>();

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Callback<string>(key => capturedKeys.Add(key))
            .Returns((List<ProductCardDto>?)null);

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters1);
        await _sut.GetAvailableProducts(parameters2);
        await _sut.GetAvailableProducts(parameters3);

        // Assert
        capturedKeys.Should().HaveCount(3);
        capturedKeys.Should().OnlyHaveUniqueItems();
        capturedKeys[0].Should().Contain("1:10");
        capturedKeys[1].Should().Contain("2:10");
        capturedKeys[2].Should().Contain("1:20");
    }

    #endregion

    #region Cache Miss Scenarios

    [Fact]
    public async Task GetAvailableProducts_ShouldFetchFromRepository_WhenCacheMiss()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var products = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Product 1", 99.99m)
        };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().BeSameAs(products);
        _mockShopRepository.Verify(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()), Times.Once);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldCacheResult_WhenCacheMiss()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var products = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Product 1", 99.99m)
        };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert - Should cache with 3 minutes absolute and sliding expiration
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(),
            products,
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldCacheEmptyList_WhenNoProductsExist()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };

        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Returns((List<ProductCardDto>?)null);

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert - Empty lists should also be cached
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(),
            It.Is<List<ProductCardDto>>(list => list.Count == 0),
            It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>()), Times.Once);
    }

    #endregion

    #region Search Query Scenarios (No Caching)

    [Fact]
    public async Task GetAvailableProducts_ShouldNotUseCache_WhenSearchValueProvided()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SearchBy = "Title",
            SearchValue = "Laptop"
        };

        var products = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Gaming Laptop", 999.99m)
        };

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        // Cache should NOT be checked or set for search queries
        _mockCacheService.Verify(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<List<ProductCardDto>>(), 
            It.IsAny<TimeSpan>(), 
            It.IsAny<TimeSpan>()), Times.Never);

        // Repository should be called directly
        _mockShopRepository.Verify(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()), Times.Once);
    }

    [Theory]
    [InlineData("Laptop")]
    [InlineData("Phone")]
    [InlineData("ABC123")]
    public async Task GetAvailableProducts_ShouldSkipCache_ForAllSearchQueries(string searchValue)
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SearchBy = "Title",
            SearchValue = searchValue
        };

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        _mockCacheService.Verify(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<List<ProductCardDto>>(), 
            It.IsAny<TimeSpan>(), 
            It.IsAny<TimeSpan>()), Times.Never);
    }

    #endregion

    #region Cache Key Building Tests

    [Fact]
    public async Task GetAvailableProducts_ShouldUseDefaultInCacheKey_WhenSortByIsNull()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SortBy = null,
            SortOrder = null
        };

        string? capturedKey = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Callback<string>(key => capturedKey = key)
            .Returns((List<ProductCardDto>?)null);

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedKey.Should().Be("Shop:GetAvailableProducts:1:10:default:default");
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldUseDefaultInCacheKey_WhenSortByIsEmpty()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SortBy = "",
            SortOrder = ""
        };

        string? capturedKey = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Callback<string>(key => capturedKey = key)
            .Returns((List<ProductCardDto>?)null);

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedKey.Should().Be("Shop:GetAvailableProducts:1:10:default:default");
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldIncludeSortParametersInCacheKey()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SortBy = "Title-Ascending"
        };

        string? capturedKey = null;
        _mockCacheService.Setup(c => c.Get<List<ProductCardDto>>(It.IsAny<string>()))
            .Callback<string>(key => capturedKey = key)
            .Returns((List<ProductCardDto>?)null);

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedKey.Should().Be("Shop:GetAvailableProducts:1:10:Title:Ascending");
    }

    #endregion

    #endregion

    #region Helper Methods

    private static ProductCardDto CreateProductCardDto(int id, string title, decimal price, decimal? discount = null)
    {
        return new ProductCardDto
        {
            Id = id,
            Title = title,
            Description = $"Description for {title}",
            Price = price,
            Discount = discount,
            ImageName = $"product-{id}.jpg"
        };
    }

    #endregion
}
