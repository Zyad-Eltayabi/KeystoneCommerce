using KeystoneCommerce.Application.DTOs.Review;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ReviewServiceTests")]
public class ReviewServiceTest
{
    private readonly Mock<IReviewRepository> _mockReviewRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly IMappingService _mappingService;
    private readonly ReviewService _sut;

    public ReviewServiceTest()
    {
        _mockReviewRepository = new Mock<IReviewRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mappingService = new MappingService(MapperHelper.CreateMapper());

        _sut = new ReviewService(
            _mockReviewRepository.Object,
            _mappingService,
            _mockProductRepository.Object);
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
