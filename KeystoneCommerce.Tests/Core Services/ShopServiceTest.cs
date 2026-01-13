using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ShopServiceTests")]
public class ShopServiceTest
{
    private readonly Mock<IShopRepository> _mockShopRepository;
    private readonly ShopService _sut;

    public ShopServiceTest()
    {
        _mockShopRepository = new Mock<IShopRepository>();
        _sut = new ShopService(_mockShopRepository.Object);
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

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(products);

        _mockShopRepository.Verify(r => r.GetAvailableProducts(parameters), Times.Once);
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldReturnEmptyList_WhenNoProductsExist()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableProducts_ShouldPassCorrectParameters()
    {
        // Arrange
        var parameters = new PaginationParameters
        {
            PageNumber = 2,
            PageSize = 15,
            SortBy = "Price",
            SortOrder = "Descending",
            SearchBy = "Title",
            SearchValue = "Test"
        };

        PaginationParameters? capturedParameters = null;
        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .Callback<PaginationParameters>(p => capturedParameters = p)
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.PageNumber.Should().Be(2);
        capturedParameters.PageSize.Should().Be(15);
        capturedParameters.SortBy.Should().Be("Price");
        capturedParameters.SortOrder.Should().Be("Descending");
        capturedParameters.SearchBy.Should().Be("Title");
        capturedParameters.SearchValue.Should().Be("Test");
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

    #region Product Data Scenarios

    [Fact]
    public async Task GetAvailableProducts_ShouldReturnProductsWithDiscounts()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var products = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Discounted Product", 100m, 20m)
        };

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

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAvailableProducts(parameters);

        // Assert
        result.Should().HaveCount(1);
        result[0].Discount.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetAvailableProducts_ShouldHandleLargePageSize()
    {
        // Arrange - PageSize max is 30 as per PaginationParameters
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 100 };

        _mockShopRepository.Setup(r => r.GetAvailableProducts(It.IsAny<PaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAvailableProducts(parameters);

        // Assert
        // PageSize should be capped at 30
        parameters.PageSize.Should().Be(30);
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
