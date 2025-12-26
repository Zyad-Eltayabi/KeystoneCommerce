using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.ProductDetails;
using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ProductDetailsServiceTests")]
public class ProductDetailsServiceTest
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IProductDetailsRepository> _mockProductDetailsRepository;
    private readonly Mock<ILogger<ProductDetailsService>> _mockLogger;
    private readonly IMappingService _mappingService;
    private readonly ProductDetailsService _service;

    public ProductDetailsServiceTest()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockProductDetailsRepository = new Mock<IProductDetailsRepository>();
        _mockLogger = new Mock<ILogger<ProductDetailsService>>();
        _mappingService = new MappingService(MapperHelper.CreateMapper());

        _service = new ProductDetailsService(
            _mockProductRepository.Object,
            _mockProductDetailsRepository.Object,
            _mockLogger.Object,
            _mappingService);
    }

    #region Happy Path Scenarios

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WhenProductExistsAndHasNewArrivals()
    {
        // Arrange
        int productId = 1;
        var product = CreateSampleProduct(productId);
        var newArrivals = CreateSampleNewArrivals(3);

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync(product);
        _mockProductDetailsRepository.Setup(r => r.GetNewArrivalsExcludingProduct(productId))
            .ReturnsAsync(newArrivals);

        // Act
        var result = await _service.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Product.Should().NotBeNull();
        result.Product.Id.Should().Be(productId);
        result.Product.Title.Should().Be(product.Title);
        result.NewArrivals.Should().NotBeNull();
        result.NewArrivals.Should().HaveCount(3);

        _mockProductRepository.Verify(r => r.GetProductByIdAsync(productId), Times.Once);
        _mockProductDetailsRepository.Verify(r => r.GetNewArrivalsExcludingProduct(productId), Times.Once);
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WhenProductExistsWithoutNewArrivals()
    {
        // Arrange
        int productId = 1;
        var product = CreateSampleProduct(productId);

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync(product);
        _mockProductDetailsRepository.Setup(r => r.GetNewArrivalsExcludingProduct(productId))
            .ReturnsAsync((List<ProductCardDto>?)null);

        // Act
        var result = await _service.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Product.Should().NotBeNull();
        result.Product.Id.Should().Be(productId);
        result.NewArrivals.Should().NotBeNull();
        result.NewArrivals.Should().BeEmpty();

        _mockProductRepository.Verify(r => r.GetProductByIdAsync(productId), Times.Once);
        _mockProductDetailsRepository.Verify(r => r.GetNewArrivalsExcludingProduct(productId), Times.Once);
    }

    [Fact]
    public async Task GetProductDetails_ShouldReturnProductDetailsDto_WhenProductExistsWithEmptyNewArrivals()
    {
        // Arrange
        int productId = 1;
        var product = CreateSampleProduct(productId);
        var emptyNewArrivals = new List<ProductCardDto>();

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync(product);
        _mockProductDetailsRepository.Setup(r => r.GetNewArrivalsExcludingProduct(productId))
            .ReturnsAsync(emptyNewArrivals);

        // Act
        var result = await _service.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Product.Should().NotBeNull();
        result.NewArrivals.Should().NotBeNull();
        result.NewArrivals.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public async Task GetProductDetails_ShouldHandleDifferentProductIds_Successfully(int productId)
    {
        // Arrange
        var product = CreateSampleProduct(productId);
        var newArrivals = CreateSampleNewArrivals(2);

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync(product);
        _mockProductDetailsRepository.Setup(r => r.GetNewArrivalsExcludingProduct(productId))
            .ReturnsAsync(newArrivals);

        // Act
        var result = await _service.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Product.Id.Should().Be(productId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetProductDetails_ShouldHandleDifferentNewArrivalsCounts_Successfully(int count)
    {
        // Arrange
        int productId = 1;
        var product = CreateSampleProduct(productId);
        var newArrivals = CreateSampleNewArrivals(count);

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync(product);
        _mockProductDetailsRepository.Setup(r => r.GetNewArrivalsExcludingProduct(productId))
            .ReturnsAsync(newArrivals);

        // Act
        var result = await _service.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.NewArrivals.Should().HaveCount(count);
    }

    #endregion

    #region Null and Not Found Scenarios

    [Fact]
    public async Task GetProductDetails_ShouldReturnNull_WhenProductNotFound()
    {
        // Arrange
        int productId = 999;

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductDetails(productId);

        // Assert
        result.Should().BeNull();

        _mockProductRepository.Verify(r => r.GetProductByIdAsync(productId), Times.Once);
        _mockProductDetailsRepository.Verify(r => r.GetNewArrivalsExcludingProduct(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetProductDetails_ShouldReturnNull_WhenProductIdIsInvalid(int productId)
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductDetails(productId);

        // Assert
        result.Should().BeNull();
        _mockProductDetailsRepository.Verify(r => r.GetNewArrivalsExcludingProduct(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task GetProductDetails_ShouldCallRepositoriesInCorrectOrder()
    {
        // Arrange
        int productId = 1;
        var product = CreateSampleProduct(productId);
        var newArrivals = CreateSampleNewArrivals(2);
        var callOrder = new List<string>();

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .Callback(() => callOrder.Add("GetProduct"))
            .ReturnsAsync(product);

        _mockProductDetailsRepository.Setup(r => r.GetNewArrivalsExcludingProduct(productId))
            .Callback(() => callOrder.Add("GetNewArrivals"))
            .ReturnsAsync(newArrivals);

        // Act
        await _service.GetProductDetails(productId);

        // Assert
        callOrder.Should().Equal("GetProduct", "GetNewArrivals");
    }

    [Fact]
    public async Task GetProductDetails_ShouldNotCallNewArrivalsRepository_WhenProductNotFound()
    {
        // Arrange
        int productId = 999;

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        await _service.GetProductDetails(productId);

        // Assert
        _mockProductDetailsRepository.Verify(r => r.GetNewArrivalsExcludingProduct(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetProductDetails_ShouldHandleNullNewArrivals_ByReturningEmptyList()
    {
        // Arrange
        int productId = 1;
        var product = CreateSampleProduct(productId);

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId))
            .ReturnsAsync(product);
        _mockProductDetailsRepository.Setup(r => r.GetNewArrivalsExcludingProduct(productId))
            .ReturnsAsync((List<ProductCardDto>?)null);

        // Act
        var result = await _service.GetProductDetails(productId);

        // Assert
        result.Should().NotBeNull();
        result!.NewArrivals.Should().NotBeNull();
        result.NewArrivals.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static Product CreateSampleProduct(int id)
    {
        return new Product
        {
            Id = id,
            Title = $"Product {id}",
            Summary = $"Summary for product {id}",
            Description = $"Description for product {id}",
            Price = 99.99m,
            Discount = 10m,
            QTY = 50,
            Tags = "tag1,tag2",
            ImageName = $"product-{id}.jpg",
            Galleries = new List<ProductGallery>
            {
                new() { Id = 1, ImageName = "gallery1.jpg", ProductId = id },
                new() { Id = 2, ImageName = "gallery2.jpg", ProductId = id }
            }
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
