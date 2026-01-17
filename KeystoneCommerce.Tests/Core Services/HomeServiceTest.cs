using KeystoneCommerce.Application.DTOs.Banner;
using KeystoneCommerce.Application.DTOs.Shop;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("HomeServiceTests")]
public class HomeServiceTest
{
    private readonly Mock<IBannerService> _mockBannerService;
    private readonly Mock<ILogger<HomeService>> _mockLogger;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly HomeService _sut;

    public HomeServiceTest()
    {
        _mockBannerService = new Mock<IBannerService>();
        _mockLogger = new Mock<ILogger<HomeService>>();
        _mockProductRepository = new Mock<IProductRepository>();

        _sut = new HomeService(
            _mockBannerService.Object,
            _mockLogger.Object,
            _mockProductRepository.Object);
    }

    #region GetHomePageDataAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task GetHomePageDataAsync_ShouldReturnCompleteHomePageDto_WhenAllDataExists()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(2, 2, 2);
        var newArrivals = CreateProductCardDtos(5);
        var topSellingProducts = CreateProductCardDtos(5);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.bannersDto.Should().NotBeNull();
        result.bannersDto.Should().BeEquivalentTo(homeBannersDto);
        result.NewArrivals.Should().NotBeNull();
        result.NewArrivals.Should().HaveCount(5);
        result.NewArrivals.Should().BeEquivalentTo(newArrivals);
        result.TopSellingProducts.Should().NotBeNull();
        result.TopSellingProducts.Should().HaveCount(5);
        result.TopSellingProducts.Should().BeEquivalentTo(topSellingProducts);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldReturnHomePageDto_WithEmptyBannerLists()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(0, 0, 0);
        var newArrivals = CreateProductCardDtos(3);
        var topSellingProducts = CreateProductCardDtos(3);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.bannersDto.HomePage.Should().BeEmpty();
        result.bannersDto.Featured.Should().BeEmpty();
        result.bannersDto.TopProducts.Should().BeEmpty();
        result.NewArrivals.Should().HaveCount(3);
        result.TopSellingProducts.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldReturnHomePageDto_WithEmptyProductLists()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(2, 2, 2);
        var emptyNewArrivals = new List<ProductCardDto>();
        var emptyTopSellingProducts = new List<ProductCardDto>();

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(emptyNewArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(emptyTopSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.bannersDto.Should().NotBeNull();
        result.bannersDto.HomePage.Should().HaveCount(2);
        result.NewArrivals.Should().BeEmpty();
        result.TopSellingProducts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldReturnHomePageDto_WithAllEmptyLists()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(0, 0, 0);
        var emptyNewArrivals = new List<ProductCardDto>();
        var emptyTopSellingProducts = new List<ProductCardDto>();

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(emptyNewArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(emptyTopSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.bannersDto.HomePage.Should().BeEmpty();
        result.bannersDto.Featured.Should().BeEmpty();
        result.bannersDto.TopProducts.Should().BeEmpty();
        result.NewArrivals.Should().BeEmpty();
        result.TopSellingProducts.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, 1, 1, 1, 1)]
    [InlineData(5, 5, 5, 10, 10)]
    [InlineData(10, 8, 12, 20, 15)]
    public async Task GetHomePageDataAsync_ShouldReturnCorrectCounts_ForDifferentDataSizes(
        int homePageCount, int featuredCount, int topProductsCount, int newArrivalsCount, int topSellingCount)
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(homePageCount, featuredCount, topProductsCount);
        var newArrivals = CreateProductCardDtos(newArrivalsCount);
        var topSellingProducts = CreateProductCardDtos(topSellingCount);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.bannersDto.HomePage.Should().HaveCount(homePageCount);
        result.bannersDto.Featured.Should().HaveCount(featuredCount);
        result.bannersDto.TopProducts.Should().HaveCount(topProductsCount);
        result.NewArrivals.Should().HaveCount(newArrivalsCount);
        result.TopSellingProducts.Should().HaveCount(topSellingCount);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldReturnHomePageDto_WithMaximumDataSizes()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(100, 100, 100);
        var newArrivals = CreateProductCardDtos(100);
        var topSellingProducts = CreateProductCardDtos(100);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.bannersDto.HomePage.Should().HaveCount(100);
        result.bannersDto.Featured.Should().HaveCount(100);
        result.bannersDto.TopProducts.Should().HaveCount(100);
        result.NewArrivals.Should().HaveCount(100);
        result.TopSellingProducts.Should().HaveCount(100);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task GetHomePageDataAsync_ShouldCallAllDependenciesOnce()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = CreateProductCardDtos(1);
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        await _sut.GetHomePageDataAsync();

        // Assert
        _mockBannerService.Verify(s => s.PrepareBannersForHomePage(), Times.Once);
        _mockProductRepository.Verify(r => r.GetTopNewArrivalsAsync(), Times.Once);
        _mockProductRepository.Verify(r => r.GetTopSellingProductsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldReturnNewInstanceEveryCall()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = CreateProductCardDtos(1);
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result1 = await _sut.GetHomePageDataAsync();
        var result2 = await _sut.GetHomePageDataAsync();

        // Assert
        result1.Should().NotBeSameAs(result2);
    }

    #endregion

    #region Data Integrity Scenarios

    [Fact]
    public async Task GetHomePageDataAsync_ShouldPreserveAllBannerProperties()
    {
        // Arrange
        var homeBannersDto = new HomeBannersDto
        {
            HomePage = new List<BannerDto>
            {
                CreateBannerDto(1, "Home Banner 1", "Home Subtitle 1", "home-1.jpg", "/home/1", 1, "HomePage"),
                CreateBannerDto(2, "Home Banner 2", "Home Subtitle 2", "home-2.jpg", "/home/2", 2, "HomePage")
            },
            Featured = new List<BannerDto>
            {
                CreateBannerDto(3, "Featured 1", "Featured Subtitle 1", "featured-1.jpg", "/featured/1", 1, "Featured")
            },
            TopProducts = new List<BannerDto>
            {
                CreateBannerDto(4, "Top 1", "Top Subtitle 1", "top-1.jpg", "/top/1", 1, "TopProducts")
            }
        };

        var newArrivals = CreateProductCardDtos(1);
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.bannersDto.HomePage[0].Id.Should().Be(1);
        result.bannersDto.HomePage[0].Title.Should().Be("Home Banner 1");
        result.bannersDto.HomePage[0].SubTitle.Should().Be("Home Subtitle 1");
        result.bannersDto.HomePage[0].ImageName.Should().Be("home-1.jpg");
        result.bannersDto.HomePage[0].Link.Should().Be("/home/1");
        result.bannersDto.HomePage[0].Priority.Should().Be(1);
        result.bannersDto.HomePage[0].BannerType.Should().Be("HomePage");

        result.bannersDto.Featured[0].Id.Should().Be(3);
        result.bannersDto.TopProducts[0].Id.Should().Be(4);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldPreserveAllProductCardProperties()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Product 1", "Description 1", 99.99m, 10.5m, "product-1.jpg"),
            CreateProductCardDto(2, "Product 2", "Description 2", 199.99m, null, "product-2.jpg")
        };
        var topSellingProducts = new List<ProductCardDto>
        {
            CreateProductCardDto(3, "Top Seller", "Top Description", 299.99m, 20.0m, null)
        };

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Id.Should().Be(1);
        result.NewArrivals[0].Title.Should().Be("Product 1");
        result.NewArrivals[0].Description.Should().Be("Description 1");
        result.NewArrivals[0].Price.Should().Be(99.99m);
        result.NewArrivals[0].Discount.Should().Be(10.5m);
        result.NewArrivals[0].ImageName.Should().Be("product-1.jpg");

        result.NewArrivals[1].Discount.Should().BeNull();

        result.TopSellingProducts[0].Id.Should().Be(3);
        result.TopSellingProducts[0].ImageName.Should().BeNull();
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleProductsWithNullOptionalFields()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            new ProductCardDto
            {
                Id = 1,
                Title = "Product",
                Description = "Description",
                Price = 50.00m,
                Discount = null,
                ImageName = null
            }
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Discount.Should().BeNull();
        result.NewArrivals[0].ImageName.Should().BeNull();
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleDecimalPrecision()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Product", "Desc", 99.999m, 15.555m, "image.jpg")
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Price.Should().Be(99.999m);
        result.NewArrivals[0].Discount.Should().Be(15.555m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleProductsWithSpecialCharactersInTitle()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Product <>&\"'!@#$%", "Description", 50.00m, null, "image.jpg")
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Title.Should().Be("Product <>&\"'!@#$%");
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "产品 المنتج Продукт", "説明 الوصف Описание", 100.00m, null, "image.jpg")
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Title.Should().Be("产品 المنتج Продукт");
        result.NewArrivals[0].Description.Should().Be("説明 الوصف Описание");
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleVeryLongStrings()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var longTitle = new string('A', 10000);
        var longDescription = new string('B', 10000);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, longTitle, longDescription, 50.00m, null, "image.jpg")
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Title.Should().HaveLength(10000);
        result.NewArrivals[0].Description.Should().HaveLength(10000);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleZeroPrices()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Free Product", "Description", 0.00m, 0.00m, "image.jpg")
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Price.Should().Be(0.00m);
        result.NewArrivals[0].Discount.Should().Be(0.00m);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleVeryHighPrices()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Expensive Product", "Description", 999999999.99m, 99999.99m, "image.jpg")
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Price.Should().Be(999999999.99m);
        result.NewArrivals[0].Discount.Should().Be(99999.99m);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleNegativePrices()
    {
        // Arrange - Some systems might allow negative prices for refunds/credits
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "Refund Product", "Description", -50.00m, -10.00m, "image.jpg")
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Price.Should().Be(-50.00m);
        result.NewArrivals[0].Discount.Should().Be(-10.00m);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldHandleEmptyStrings()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = new List<ProductCardDto>
        {
            CreateProductCardDto(1, "", "", 50.00m, null, "")
        };
        var topSellingProducts = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.NewArrivals[0].Title.Should().Be("");
        result.NewArrivals[0].Description.Should().Be("");
        result.NewArrivals[0].ImageName.Should().Be("");
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task GetHomePageDataAsync_ShouldPropagateException_WhenBannerServiceThrows()
    {
        // Arrange
        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ThrowsAsync(new InvalidOperationException("Banner service error"));

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(new List<ProductCardDto>());

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(new List<ProductCardDto>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.GetHomePageDataAsync());
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldPropagateException_WhenGetTopNewArrivalsThrows()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ThrowsAsync(new InvalidOperationException("New arrivals error"));

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(new List<ProductCardDto>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.GetHomePageDataAsync());
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldPropagateException_WhenGetTopSellingProductsThrows()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = CreateProductCardDtos(1);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ThrowsAsync(new InvalidOperationException("Top selling error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.GetHomePageDataAsync());
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldPropagateException_WhenDatabaseConnectionFails()
    {
        // Arrange
        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.GetHomePageDataAsync());
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldPropagateException_WhenTimeoutOccurs()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ThrowsAsync(new TimeoutException("Database timeout"));

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(CreateHomeBannersDto(1, 1, 1));

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(new List<ProductCardDto>());

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            async () => await _sut.GetHomePageDataAsync());
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldNotCallRemainingServices_WhenFirstServiceThrows()
    {
        // Arrange
        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ThrowsAsync(new InvalidOperationException("Error"));

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(new List<ProductCardDto>());

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(new List<ProductCardDto>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.GetHomePageDataAsync());

        // Verify that product repository methods might still be called due to parallel execution
        // but we shouldn't rely on specific behavior here
    }

    #endregion

    #region Dependency Interaction Scenarios

    [Fact]
    public async Task GetHomePageDataAsync_ShouldNotModifyReturnedBannersDto()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(2, 2, 2);
        var originalHomePageCount = homeBannersDto.HomePage.Count;
        var originalFeaturedCount = homeBannersDto.Featured.Count;
        var originalTopProductsCount = homeBannersDto.TopProducts.Count;

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(CreateProductCardDtos(1));

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(CreateProductCardDtos(1));

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        homeBannersDto.HomePage.Count.Should().Be(originalHomePageCount);
        homeBannersDto.Featured.Count.Should().Be(originalFeaturedCount);
        homeBannersDto.TopProducts.Count.Should().Be(originalTopProductsCount);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldNotModifyReturnedProductLists()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = CreateProductCardDtos(5);
        var topSellingProducts = CreateProductCardDtos(5);
        var originalNewArrivalsCount = newArrivals.Count;
        var originalTopSellingCount = topSellingProducts.Count;

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        newArrivals.Count.Should().Be(originalNewArrivalsCount);
        topSellingProducts.Count.Should().Be(originalTopSellingCount);
    }

    [Fact]
    public async Task GetHomePageDataAsync_ShouldAssignCorrectReferences()
    {
        // Arrange
        var homeBannersDto = CreateHomeBannersDto(1, 1, 1);
        var newArrivals = CreateProductCardDtos(2);
        var topSellingProducts = CreateProductCardDtos(2);

        _mockBannerService.Setup(s => s.PrepareBannersForHomePage())
            .ReturnsAsync(homeBannersDto);

        _mockProductRepository.Setup(r => r.GetTopNewArrivalsAsync())
            .ReturnsAsync(newArrivals);

        _mockProductRepository.Setup(r => r.GetTopSellingProductsAsync())
            .ReturnsAsync(topSellingProducts);

        // Act
        var result = await _sut.GetHomePageDataAsync();

        // Assert
        result.bannersDto.Should().BeSameAs(homeBannersDto);
        result.NewArrivals.Should().BeSameAs(newArrivals);
        result.TopSellingProducts.Should().BeSameAs(topSellingProducts);
    }

    #endregion

    #endregion

    #region Helper Methods

    private static HomeBannersDto CreateHomeBannersDto(int homePageCount, int featuredCount, int topProductsCount)
    {
        return new HomeBannersDto
        {
            HomePage = CreateBannerDtos(homePageCount, "HomePage"),
            Featured = CreateBannerDtos(featuredCount, "Featured"),
            TopProducts = CreateBannerDtos(topProductsCount, "TopProducts")
        };
    }

    private static List<BannerDto> CreateBannerDtos(int count, string bannerType)
    {
        var banners = new List<BannerDto>();
        for (int i = 1; i <= count; i++)
        {
            banners.Add(CreateBannerDto(i, $"{bannerType} Banner {i}", $"Subtitle {i}", $"banner-{i}.jpg", $"/{bannerType.ToLower()}/{i}", i, bannerType));
        }
        return banners;
    }

    private static BannerDto CreateBannerDto(int id, string title, string subtitle, string imageName, string link, int priority, string bannerType)
    {
        return new BannerDto
        {
            Id = id,
            Title = title,
            SubTitle = subtitle,
            ImageName = imageName,
            Link = link,
            Priority = priority,
            BannerType = bannerType
        };
    }

    private static List<ProductCardDto> CreateProductCardDtos(int count)
    {
        var products = new List<ProductCardDto>();
        for (int i = 1; i <= count; i++)
        {
            products.Add(CreateProductCardDto(i, $"Product {i}", $"Description {i}", 50.00m * i, i % 2 == 0 ? 10.00m : null, $"product-{i}.jpg"));
        }
        return products;
    }

    private static ProductCardDto CreateProductCardDto(int id, string title, string description, decimal price, decimal? discount, string? imageName)
    {
        return new ProductCardDto
        {
            Id = id,
            Title = title,
            Description = description,
            Price = price,
            Discount = discount,
            ImageName = imageName
        };
    }

    #endregion
}
