using KeystoneCommerce.Application.DTOs.ProductDetails;
using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ProductDetailsServiceTests")]
public class ProductDetailsServiceTest
{
    private readonly Mock<IProductDetailsRepository> _mockProductDetailsRepository;
    private readonly Mock<ILogger<ProductDetailsService>> _mockLogger;
    private readonly ProductDetailsService _sut;

    public ProductDetailsServiceTest()
    {
        _mockProductDetailsRepository = new Mock<IProductDetailsRepository>();
        _mockLogger = new Mock<ILogger<ProductDetailsService>>();

        _sut = new ProductDetailsService(
            _mockProductDetailsRepository.Object,
            _mockLogger.Object);
    }

    #region GetProductDetails Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WhenProductExists()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(productDetails);
        result!.Id.Should().Be(productId);
        result.Title.Should().Be($"Product {productId}");

        _mockProductDetailsRepository.Verify(r => r.GetProductDetailsByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WithAllProperties()
    {
        // Arrange
        int productId = 5;
        var productDetails = CreateSampleProductDetailsDto(productId);

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Title.Should().Be($"Product {productId}");
        result.Summary.Should().Be($"Summary for product {productId}");
        result.Description.Should().Be($"Description for product {productId}");
        result.Price.Should().Be(99.99m);
        result.Discount.Should().Be(10m);
        result.QTY.Should().Be(50);
        result.ImageName.Should().Be($"product-{productId}.jpg");
        result.Tags.Should().Be("tag1,tag2");
        result.GalleryImageNames.Should().HaveCount(2);
        result.NewArrivals.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    [InlineData(int.MaxValue)]
    public async Task GetProductDetails_ShouldHandleDifferentProductIds_Successfully(int productId)
    {
        // Arrange
        var productDetails = CreateSampleProductDetailsDto(productId);

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        _mockProductDetailsRepository.Verify(r => r.GetProductDetailsByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WithNewArrivals()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.NewArrivals = CreateSampleNewArrivals(3);

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.NewArrivals.Should().NotBeNull();
        result.NewArrivals.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WithEmptyNewArrivals()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.NewArrivals = new List<ProductCardDto>();

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.NewArrivals.Should().NotBeNull();
        result.NewArrivals.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WithNullNewArrivals()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.NewArrivals = null;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.NewArrivals.Should().BeNull();
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WithNullGalleryImages()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.GalleryImageNames = null;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.GalleryImageNames.Should().BeNull();
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WithEmptyGalleryImages()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.GalleryImageNames = new List<string>();

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.GalleryImageNames.Should().NotBeNull();
        result.GalleryImageNames.Should().BeEmpty();
    }

    #endregion

    #region Null and Not Found Scenarios

    [Fact]
    public async Task GetProductDetails_ShouldReturnNull_WhenProductNotFound()
    {
        // Arrange
        int productId = 999;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync((ProductDetailsDto?)null);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().BeNull();
        _mockProductDetailsRepository.Verify(r => r.GetProductDetailsByIdAsync(productId), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public async Task GetProductDetails_ShouldReturnNull_WhenProductIdIsInvalid(int productId)
    {
        // Arrange
        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync((ProductDetailsDto?)null);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().BeNull();
        _mockProductDetailsRepository.Verify(r => r.GetProductDetailsByIdAsync(productId), Times.Once);
    }

    #endregion

    #region Data Integrity Scenarios

    [Fact]
    public async Task GetProductDetails_ShouldNotModifyReturnedData()
    {
        // Arrange
        int productId = 1;
        var originalProductDetails = CreateSampleProductDetailsDto(productId);

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(originalProductDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(originalProductDetails);
    }

    [Fact]
    public async Task GetProductDetails_ShouldPreserveAllProductProperties()
    {
        // Arrange
        int productId = 1;
        var productDetails = new ProductDetailsDto
        {
            Id = productId,
            Title = "Test Product with Special Characters !@#$%",
            Summary = "Summary with Unicode: ????",
            Description = "Description with\nNew Lines\nand\ttabs",
            Price = 12345.67m,
            Discount = 123.45m,
            QTY = 9999,
            ImageName = "product-with-special-name_123.jpg",
            GalleryImageNames = new List<string> { "gallery1.jpg", "gallery2.jpg" },
            Tags = "tag1,tag2,tag3",
            TotalReviews = 42,
            NewArrivals = CreateSampleNewArrivals(2)
        };

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Product with Special Characters !@#$%");
        result.Summary.Should().Be("Summary with Unicode: ????");
        result.Description.Should().Be("Description with\nNew Lines\nand\ttabs");
        result.Price.Should().Be(12345.67m);
        result.Discount.Should().Be(123.45m);
        result.QTY.Should().Be(9999);
        result.GalleryImageNames.Should().HaveCount(2);
        result.Tags.Should().Be("tag1,tag2,tag3");
        result.TotalReviews.Should().Be(42);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetProductDetails_ShouldHandleProductWithMinimalData()
    {
        // Arrange
        int productId = 1;
        var productDetails = new ProductDetailsDto
        {
            Id = productId,
            Title = "A",
            Summary = "S",
            Description = "D",
            Price = 0.01m,
            Discount = null,
            QTY = 0,
            ImageName = "a.jpg",
            GalleryImageNames = null,
            Tags = null,
            TotalReviews = 0,
            NewArrivals = null
        };

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("A");
        result.Price.Should().Be(0.01m);
        result.Discount.Should().BeNull();
        result.QTY.Should().Be(0);
        result.TotalReviews.Should().Be(0);
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleProductWithMaximalData()
    {
        // Arrange
        int productId = 1;
        var longString = new string('A', 10000);
        var manyGalleries = Enumerable.Range(1, 100).Select(i => $"gallery{i}.jpg").ToList();
        var productDetails = new ProductDetailsDto
        {
            Id = productId,
            Title = longString,
            Summary = longString,
            Description = longString,
            Price = decimal.MaxValue,
            Discount = decimal.MaxValue,
            QTY = int.MaxValue,
            ImageName = "image.jpg",
            GalleryImageNames = manyGalleries,
            Tags = longString,
            TotalReviews = int.MaxValue,
            NewArrivals = CreateSampleNewArrivals(50)
        };

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.GalleryImageNames.Should().HaveCount(100);
        result.NewArrivals.Should().HaveCount(50);
        result.TotalReviews.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleProductWithSpecialCharacters()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.Title = "<script>alert('xss')</script>";
        productDetails.Description = "Test with ' \" \\ / special chars";
        productDetails.Tags = "tag1,tag<2>,tag\"3\"";

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("<script>alert('xss')</script>");
        result.Description.Should().Be("Test with ' \" \\ / special chars");
        result.Tags.Should().Be("tag1,tag<2>,tag\"3\"");
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleProductWithUnicodeCharacters()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.Title = "?? Product ???? ??";
        productDetails.Description = "?? Description ??? ??";
        productDetails.Tags = "??,tag,?????,??";

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("?? Product ???? ??");
        result.Description.Should().Be("?? Description ??? ??");
        result.Tags.Should().Be("??,tag,?????,??");
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleProductWithWhitespaceInFields()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.Title = "  Product with spaces  ";
        productDetails.Description = "\t\nDescription with whitespace\t\n";
        productDetails.Tags = " tag1 , tag2 , tag3 ";

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("  Product with spaces  ");
        result.Description.Should().Be("\t\nDescription with whitespace\t\n");
        result.Tags.Should().Be(" tag1 , tag2 , tag3 ");
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleZeroPriceAndDiscount()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.Price = 0m;
        productDetails.Discount = 0m;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().Be(0m);
        result.Discount.Should().Be(0m);
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleNegativeQuantity()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.QTY = -5;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.QTY.Should().Be(-5);
    }

    #endregion

    #region Concurrent Execution Scenarios

    [Fact]
    public async Task GetProductDetails_ShouldHandleConcurrentRequests_ForSameProduct()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act - Execute 10 concurrent requests
        var tasks = Enumerable.Range(1, 10)
            .Select(_ => _sut.GetProductDetails(productId))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result!.Id.Should().Be(productId);
        });

        _mockProductDetailsRepository.Verify(
            r => r.GetProductDetailsByIdAsync(productId),
            Times.Exactly(10));
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleConcurrentRequests_ForDifferentProducts()
    {
        // Arrange
        var productIds = Enumerable.Range(1, 10).ToList();
        foreach (var id in productIds)
        {
            var productDetails = CreateSampleProductDetailsDto(id);
            _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(id))
                .ReturnsAsync(productDetails);
        }

        // Act
        var tasks = productIds.Select(id => _sut.GetProductDetails(id)).ToList();
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            results[i].Should().NotBeNull();
            results[i]!.Id.Should().Be(productIds[i]);
        }
    }

    #endregion

    #region Repository Interaction Tests

    [Fact]
    public async Task GetProductDetails_ShouldCallRepository_ExactlyOnce()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        await _sut.GetProductDetails(productId);

        // Assert
        _mockProductDetailsRepository.Verify(
            r => r.GetProductDetailsByIdAsync(productId),
            Times.Once);
    }

    [Fact]
    public async Task GetProductDetails_ShouldCallRepository_WithCorrectParameter()
    {
        // Arrange
        int productId = 42;
        var productDetails = CreateSampleProductDetailsDto(productId);
        int? capturedProductId = null;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(It.IsAny<int>()))
            .Callback<int>(id => capturedProductId = id)
            .ReturnsAsync(productDetails);

        // Act
        await _sut.GetProductDetails(productId);

        // Assert
        capturedProductId.Should().Be(42);
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnSameInstance_FromRepository()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().BeSameAs(productDetails);
    }

    [Fact]
    public async Task GetProductDetails_ShouldCallRepositoryOnce_EvenWhenResultIsNull()
    {
        // Arrange
        int productId = 999;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync((ProductDetailsDto?)null);

        // Act
        await _sut.GetProductDetails(productId);

        // Assert
        _mockProductDetailsRepository.Verify(
            r => r.GetProductDetailsByIdAsync(productId),
            Times.Once);
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.99)]
    [InlineData(1.00)]
    [InlineData(999999.99)]
    public async Task GetProductDetails_ShouldHandleVariousPriceValues(decimal price)
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.Price = price;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().Be(price);
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleNullDiscount()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.Discount = null;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Discount.Should().BeNull();
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleSmallDiscount()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.Discount = 0.01m;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Discount.Should().Be(0.01m);
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleLargeDiscount()
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.Discount = 99.99m;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Discount.Should().Be(99.99m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(10000)]
    public async Task GetProductDetails_ShouldHandleVariousQuantityValues(int quantity)
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.QTY = quantity;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.QTY.Should().Be(quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task GetProductDetails_ShouldHandleVariousTotalReviewsValues(int totalReviews)
    {
        // Arrange
        int productId = 1;
        var productDetails = CreateSampleProductDetailsDto(productId);
        productDetails.TotalReviews = totalReviews;

        _mockProductDetailsRepository.Setup(r => r.GetProductDetailsByIdAsync(productId))
            .ReturnsAsync(productDetails);

        // Act
        var result = await _sut.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.TotalReviews.Should().Be(totalReviews);
    }

    #endregion

    #endregion

    #region Helper Methods

    private static ProductDetailsDto CreateSampleProductDetailsDto(int productId)
    {
        return new ProductDetailsDto
        {
            Id = productId,
            Title = $"Product {productId}",
            Summary = $"Summary for product {productId}",
            Description = $"Description for product {productId}",
            Price = 99.99m,
            Discount = 10m,
            QTY = 50,
            Tags = "tag1,tag2",
            ImageName = $"product-{productId}.jpg",
            GalleryImageNames = new List<string>
            {
                "gallery1.jpg",
                "gallery2.jpg"
            },
            TotalReviews = 5,
            NewArrivals = CreateSampleNewArrivals(3)
        };
    }

    private static List<ProductCardDto> CreateSampleNewArrivals(int count)
    {
        var arrivals = new List<ProductCardDto>();
        for (int i = 0; i < count; i++)
        {
            arrivals.Add(new ProductCardDto
            {
                Id = i + 100,
                Title = $"New Arrival {i + 1}",
                Description = $"Description {i + 1}",
                Price = (i + 1) * 10m,
                Discount = (i % 2 == 0) ? 5m : null,
                ImageName = $"arrival-{i + 1}.jpg"
            });
        }
        return arrivals;
    }

    #endregion
}
