using KeystoneCommerce.Application.DTOs.Review;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ReviewServiceTests")]
public class ReviewServiceTest
{
    private readonly Mock<IReviewRepository> _mockReviewRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly IMappingService _mappingService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<ReviewService>> _mockLogger;
    private readonly ReviewService _sut;

    public ReviewServiceTest()
    {
        _mockReviewRepository = new Mock<IReviewRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mappingService = new MappingService(MapperHelper.CreateMapper());
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<ReviewService>>();

        _sut = new ReviewService(
            _mockReviewRepository.Object,
            _mappingService,
            _mockProductRepository.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    #region CreateNewReview Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CreateNewReview_ShouldReturnSuccess_WhenValidReviewDto()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>()))
            .Returns(Task.CompletedTask);
        _mockReviewRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewReview(createReviewDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ProductId.Should().Be(createReviewDto.ProductId);
        result.Data.Comment.Should().Be(createReviewDto.Comment);

        _mockReviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Once);
        _mockReviewRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix($"Review:GetProductReviews:{createReviewDto.ProductId}:"), Times.Once);
    }

    [Fact]
    public async Task CreateNewReview_ShouldReturnSuccess_WithMinimumValidComment()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.Comment = "A"; // Minimum valid comment

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);
        _mockReviewRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewReview(createReviewDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCacheService.Verify(c => c.RemoveByPrefix($"Review:GetProductReviews:{createReviewDto.ProductId}:"), Times.Once);
    }

    [Fact]
    public async Task CreateNewReview_ShouldReturnSuccess_WithMaximumValidComment()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.Comment = new string('A', 2500); // Maximum valid comment

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);
        _mockReviewRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewReview(createReviewDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCacheService.Verify(c => c.RemoveByPrefix($"Review:GetProductReviews:{createReviewDto.ProductId}:"), Times.Once);
    }

    [Fact]
    public async Task CreateNewReview_ShouldMapReviewCorrectly()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        Review? capturedReview = null;

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>()))
            .Callback<Review>(r => capturedReview = r)
            .Returns(Task.CompletedTask);
        _mockReviewRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.CreateNewReview(createReviewDto);

        // Assert
        capturedReview.Should().NotBeNull();
        capturedReview!.ProductId.Should().Be(createReviewDto.ProductId);
        capturedReview.UserId.Should().Be(createReviewDto.UserId);
        capturedReview.Comment.Should().Be(createReviewDto.Comment);
        capturedReview.UserFullName.Should().Be(createReviewDto.UserFullName);
    }

    #endregion

    #region Validation Failure Scenarios

    [Theory]
    [MemberData(nameof(TestData.InvalidStrings), MemberType = typeof(TestData))]
    public async Task CreateNewReview_ShouldReturnFailure_WhenCommentIsNullOrWhitespace(string? comment)
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.Comment = comment!;

        // Act
        var result = await _sut.CreateNewReview(createReviewDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Comment can't be null or empty");

        _mockProductRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()), Times.Never);
        _mockReviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewReview_ShouldReturnFailure_WhenCommentExceedsMaxLength()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.Comment = new string('A', 2501); // Exceeds 2500 limit

        // Act
        var result = await _sut.CreateNewReview(createReviewDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Comment can't be longer than 2500 characters");

        _mockProductRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewReview_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateNewReview(createReviewDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Product does not exist");

        _mockReviewRepository.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateNewReview_ShouldHandleSpecialCharactersInComment()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.Comment = "Great product! 😀 <script>alert('test')</script> & \"quotes\"";

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);
        _mockReviewRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewReview(createReviewDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Comment.Should().Be(createReviewDto.Comment);
        _mockCacheService.Verify(c => c.RemoveByPrefix($"Review:GetProductReviews:{createReviewDto.ProductId}:"), Times.Once);
    }

    [Fact]
    public async Task CreateNewReview_ShouldHandleUnicodeCharactersInComment()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.Comment = "مراجعة رائعة - 优秀的产品 - 素晴らしい製品";

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);
        _mockReviewRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewReview(createReviewDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCacheService.Verify(c => c.RemoveByPrefix($"Review:GetProductReviews:{createReviewDto.ProductId}:"), Times.Once);
    }

    #endregion

    #endregion

    #region GetProductReviews Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task GetProductReviews_ShouldReturnPaginatedResult_WhenReviewsExist()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var reviews = new List<Review>
        {
            CreateReview(1, 1),
            CreateReview(2, 1)
        };

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _sut.GetProductReviews(parameters);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetProductReviews_ShouldReturnEmptyList_WhenNoReviews()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetProductReviews(parameters);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductReviews_ShouldReturnNull_WhenRepositoryReturnsNull()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync((List<Review>?)null);

        // Act
        var result = await _sut.GetProductReviews(parameters);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Pagination Scenarios

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(1, 20)]
    public async Task GetProductReviews_ShouldReturnCorrectPaginationParameters(int pageNumber, int pageSize)
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = pageNumber, PageSize = pageSize };

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetProductReviews(parameters);

        // Assert
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
    }

    #endregion

    #endregion

    #region Caching Tests

    #region GetProductReviews Caching Tests

    [Fact]
    public async Task GetProductReviews_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var cachedResult = new PaginatedResult<ReviewDto>
        {
            Items = new List<ReviewDto>
            {
                new() { Id = 1, ProductId = 1, Comment = "Cached Review", UserFullName = "Cached User" }
            },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _mockCacheService.Setup(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()))
            .Returns(cachedResult);

        // Act
        var result = await _sut.GetProductReviews(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedResult);
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()), Times.Once);
        _mockReviewRepository.Verify(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()), Times.Never);
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(),
            It.IsAny<PaginatedResult<ReviewDto>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetProductReviews_ShouldFetchFromDatabaseAndCache_WhenCacheMiss()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var reviews = new List<Review>
        {
            CreateReview(1, 1),
            CreateReview(2, 1)
        };

        _mockCacheService.Setup(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ReviewDto>?)null);

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _sut.GetProductReviews(parameters);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()), Times.Once);
        _mockReviewRepository.Verify(r => r.GetPagedAsync(parameters), Times.Once);
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(),
            It.IsAny<PaginatedResult<ReviewDto>>(),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    [Fact]
    public async Task GetProductReviews_ShouldUseCorrectCacheKey_WithAllParameters()
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 2,
            PageSize = 20,
            SortBy = "CreatedAt",
            SortOrder = "DESC",
            SearchBy = "Comment",
            SearchValue = "test"
        };
        // When SearchBy is not "ProductId", productIdFilter defaults to "all"
        var expectedCacheKey = "Review:GetProductReviews:all:2:20:CreatedAt:DESC:Comment:test";

        _mockCacheService.Setup(c => c.Get<PaginatedResult<ReviewDto>>(expectedCacheKey))
            .Returns((PaginatedResult<ReviewDto>?)null);

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(new List<Review>());

        // Act
        await _sut.GetProductReviews(parameters);

        // Assert
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ReviewDto>>(expectedCacheKey), Times.Once);
        _mockCacheService.Verify(c => c.Set(
            expectedCacheKey,
            It.IsAny<PaginatedResult<ReviewDto>>(),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    [Fact]
    public async Task GetProductReviews_ShouldSetCorrectCacheExpiration()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var reviews = new List<Review> { CreateReview(1, 1) };

        _mockCacheService.Setup(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ReviewDto>?)null);

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(reviews);

        // Act
        await _sut.GetProductReviews(parameters);

        // Assert
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(),
            It.IsAny<PaginatedResult<ReviewDto>>(),
            TimeSpan.FromMinutes(5),  // 5 minutes absolute expiration
            TimeSpan.FromMinutes(3)), // 3 minutes sliding expiration
            Times.Once);
    }

    [Theory]
    [InlineData(1, 10, "", "", "", "")]
    [InlineData(2, 20, "CreatedAt", "DESC", "", "")]
    [InlineData(1, 5, "", "", "Comment", "test")]
    [InlineData(1, 10, "", "", "ProductId", "123")]
    public async Task GetProductReviews_ShouldGenerateUniqueCacheKeys_ForDifferentParameters(
        int pageNumber, int pageSize, string sortBy, string sortOrder, string searchBy, string searchValue)
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortOrder = sortOrder,
            SearchBy = searchBy,
            SearchValue = searchValue
        };

        _mockCacheService.Setup(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ReviewDto>?)null);

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(new List<Review>());

        // When SearchBy is "ProductId", use searchValue; otherwise "all"
        var productIdFilter = searchBy?.Equals("ProductId", StringComparison.OrdinalIgnoreCase) == true 
            ? searchValue ?? "all" 
            : "all";
        var expectedCacheKey = $"Review:GetProductReviews:{productIdFilter}:{pageNumber}:{pageSize}:{sortBy}:{sortOrder}:{searchBy}:{searchValue}";

        // Act
        await _sut.GetProductReviews(parameters);

        // Assert
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ReviewDto>>(expectedCacheKey), Times.Once);
    }

    [Fact]
    public async Task GetProductReviews_ShouldNotCache_WhenRepositoryReturnsNull()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };

        _mockCacheService.Setup(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ReviewDto>?)null);

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync((List<Review>?)null);

        // Act
        var result = await _sut.GetProductReviews(parameters);

        // Assert
        result.Should().BeNull();
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(),
            It.IsAny<PaginatedResult<ReviewDto>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }

    #endregion

    #region CreateNewReview Cache Invalidation Tests

    [Fact]
    public async Task CreateNewReview_ShouldInvalidateProductSpecificCaches_OnSuccess()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.ProductId = 123; // Specific product

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>()))
            .Returns(Task.CompletedTask);
        _mockReviewRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.CreateNewReview(createReviewDto);

        // Assert - Only invalidates caches for ProductId 123
        _mockCacheService.Verify(c => c.RemoveByPrefix($"Review:GetProductReviews:{createReviewDto.ProductId}:"), Times.Once);
    }

    [Fact]
    public async Task CreateNewReview_ShouldNotInvalidateCache_WhenValidationFails()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.Comment = ""; // Invalid comment

        // Act
        await _sut.CreateNewReview(createReviewDto);

        // Assert
        _mockCacheService.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewReview_ShouldNotInvalidateCache_WhenProductDoesNotExist()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);

        // Act
        await _sut.CreateNewReview(createReviewDto);

        // Assert
        _mockCacheService.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewReview_ShouldInvalidateCacheAfterSavingToDatabase()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        var callOrder = new List<string>();

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);

        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);

        _mockReviewRepository.Setup(r => r.SaveChangesAsync())
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        _mockCacheService.Setup(c => c.RemoveByPrefix(It.IsAny<string>()))
            .Callback(() => callOrder.Add("RemoveByPrefix"));

        // Act
        await _sut.CreateNewReview(createReviewDto);

        // Assert
        callOrder.Should().Equal("AddAsync", "SaveChanges", "RemoveByPrefix");
    }

    [Fact]
    public async Task CreateNewReview_ShouldOnlyInvalidateSpecificProductCache_NotOtherProducts()
    {
        // Arrange
        var createReviewDto = CreateValidReviewDto();
        createReviewDto.ProductId = 456; // Creating review for product 456

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);
        _mockReviewRepository.Setup(r => r.AddAsync(It.IsAny<Review>()))
            .Returns(Task.CompletedTask);
        _mockReviewRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.CreateNewReview(createReviewDto);

        // Assert - Should only invalidate product 456, not 123 or others
        _mockCacheService.Verify(c => c.RemoveByPrefix("Review:GetProductReviews:456:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Review:GetProductReviews:123:"), Times.Never);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Review:GetProductReviews:789:"), Times.Never);
    }

    [Fact]
    public async Task GetProductReviews_ShouldCacheSeparately_ForDifferentProducts()
    {
        // Arrange - Request reviews for product 123
        var parametersProduct123 = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchBy = "ProductId",
            SearchValue = "123"
        };

        var reviewsProduct123 = new List<Review> { CreateReview(1, 123) };

        _mockCacheService.Setup(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ReviewDto>?)null);

        _mockReviewRepository.Setup(r => r.GetPagedAsync(parametersProduct123))
            .ReturnsAsync(reviewsProduct123);

        // Act
        await _sut.GetProductReviews(parametersProduct123);

        // Assert - Cache key should include product ID 123
        _mockCacheService.Verify(c => c.Set(
            "Review:GetProductReviews:123:1:10:::ProductId:123",
            It.IsAny<PaginatedResult<ReviewDto>>(),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    [Fact]
    public async Task GetProductReviews_ShouldUseDefaultFilter_WhenProductIdNotSpecified()
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchBy = "Comment", // Not filtering by ProductId
            SearchValue = "test"
        };

        _mockCacheService.Setup(c => c.Get<PaginatedResult<ReviewDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ReviewDto>?)null);

        _mockReviewRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(new List<Review>());

        // Act
        await _sut.GetProductReviews(parameters);

        // Assert - Cache key should use "all" as product filter
        _mockCacheService.Verify(c => c.Set(
            "Review:GetProductReviews:all:1:10:::Comment:test",
            It.IsAny<PaginatedResult<ReviewDto>>(),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    #endregion

    #endregion

    #region Helper Methods

    private static CreateReviewDto CreateValidReviewDto()
    {
        return new CreateReviewDto
        {
            ProductId = 1,
            Comment = "This is a great product!",
            UserId = "user-123",
            UserFullName = "John Doe"
        };
    }

    private static Review CreateReview(int id, int productId)
    {
        return new Review
        {
            Id = id,
            ProductId = productId,
            UserId = "user-123",
            Comment = "Test comment",
            UserFullName = "Test User"
        };
    }

    #endregion
}
