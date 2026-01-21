using KeystoneCommerce.Application.DTOs.Common;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Infrastructure.Validation.Validators.Product;
using KeystoneCommerce.Shared.Constants;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ProductServiceTests")]
public class ProductServiceTest
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IImageService> _mockImageService;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly IApplicationValidator<CreateProductDto> _createValidator;
    private readonly IApplicationValidator<UpdateProductDto> _updateValidator;
    private readonly IMappingService _mappingService;
    private readonly ProductService _sut;

    public ProductServiceTest()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockImageService = new Mock<IImageService>();
        _mockLogger = new Mock<ILogger<ProductService>>();
        _mockCacheService = new Mock<ICacheService>();

        var createFluentValidator = new CreateProductValidator();
        _createValidator = new FluentValidationAdapter<CreateProductDto>(createFluentValidator);

        var updateFluentValidator = new UpdateProductDtoValidator();
        _updateValidator = new FluentValidationAdapter<UpdateProductDto>(updateFluentValidator);

        _mappingService = new MappingService(MapperHelper.CreateMapper());

        _sut = new ProductService(
            _createValidator,
            _mockProductRepository.Object,
            _mockImageService.Object,
            _mappingService,
            _mockLogger.Object,
            _updateValidator,
            _mockCacheService.Object);
    }

    #region CreateProduct Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CreateProduct_ShouldReturnSuccess_WhenValidProductDto()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockImageService.Setup(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("saved-image.jpg");
        _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        _mockProductRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        
        // Verify cache invalidation
        _mockCacheService.Verify(c => c.Remove("HomePage:Data"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Products:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Shop:GetAvailableProducts:"), Times.Once);
    }

    [Fact]
    public async Task CreateProduct_ShouldSaveMainImageAndGalleries()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Gallaries =
        [
            CreateImageDto("gallery1"),
            CreateImageDto("gallery2")
        ];

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockImageService.Setup(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("saved-image.jpg");
        _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Main image + 2 gallery images = 3 calls
        _mockImageService.Verify(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(3));
    }

    #endregion

    #region Validation Failure Scenarios

    [Theory]
    [MemberData(nameof(TestData.InvalidStrings), MemberType = typeof(TestData))]
    public async Task CreateProduct_ShouldReturnFailure_WhenTitleIsInvalid(string? title)
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Title = title!;

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Title is required.");

        _mockProductRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Title = new string('A', 201); // Max is 200

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Title cannot exceed 200 characters.");
    }

    [Theory]
    [MemberData(nameof(TestData.InvalidStrings), MemberType = typeof(TestData))]
    public async Task CreateProduct_ShouldReturnFailure_WhenSummaryIsInvalid(string? summary)
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Summary = summary!;

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Summary is required.");
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenSummaryExceedsMaxLength()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Summary = new string('A', 501); // Max is 500

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Summary cannot exceed 500 characters.");
    }

    [Theory]
    [MemberData(nameof(TestData.InvalidStrings), MemberType = typeof(TestData))]
    public async Task CreateProduct_ShouldReturnFailure_WhenDescriptionIsInvalid(string? description)
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Description = description!;

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Description is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task CreateProduct_ShouldReturnFailure_WhenPriceIsNotPositive(decimal price)
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Price = price;

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Price must be greater than 0.");
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenDiscountIsNegative()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Discount = -10;

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Discount cannot be negative.");
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenQuantityIsNegative()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.QTY = -1;

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Quantity cannot be negative.");
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenMainImageIsNull()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.MainImage = null!;

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Main image is required.");
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenGalleriesAreEmpty()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Gallaries = [];

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("At least one gallery image is required.");
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenGalleriesExceedMax()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();
        createProductDto.Gallaries = Enumerable.Range(1, FileSizes.MaxNumberOfGalleryImages + 1)
            .Select(i => CreateImageDto($"gallery{i}"))
            .ToList();

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain($"You can upload a maximum of {FileSizes.MaxNumberOfGalleryImages} images.");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenDuplicateTitleExists()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("The product you try add it is already exist.");

        _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockImageService.Setup(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("saved-image.jpg");
        _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.CreateProduct(createProductDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to create product.");
    }

    #endregion

    #endregion

    #region GetAllProducts Tests

    [Fact]
    public async Task GetAllProducts_ShouldReturnProducts_WhenProductsExist()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct(1, "Product 1"),
            CreateProduct(2, "Product 2")
        };

        _mockProductRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        // Act
        var result = await _sut.GetAllProducts();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturnEmptyList_WhenNoProductsExist()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        // Act
        var result = await _sut.GetAllProducts();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturnEmptyList_WhenRepositoryReturnsNull()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetAllAsync()).ReturnsAsync((IEnumerable<Product>?)null);

        // Act
        var result = await _sut.GetAllProducts();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetProductByIdAsync Tests

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var product = CreateProduct(1, "Test Product");

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);

        // Act
        var result = await _sut.GetProductByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Title.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductNotFound()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetProductByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);

        // Act
        var result = await _sut.GetProductByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateProduct Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task UpdateProduct_ShouldReturnSuccess_WhenValidUpdate()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries =
        [
            new() { Id = 1, ImageName = "gallery1.jpg" }
        ];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateProduct(updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockProductRepository.Verify(r => r.Update(It.IsAny<Product>()), Times.Once);
        
        // Verify cache invalidation
        _mockCacheService.Verify(c => c.Remove("HomePage:Data"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Products:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Shop:GetAvailableProducts:"), Times.Once);
    }

    #endregion

    #region Validation Failure Scenarios

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateProduct_ShouldReturnFailure_WhenIdIsInvalid(int id)
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        updateDto.Id = id;

        // Act
        var result = await _sut.UpdateProduct(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid proudct id");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task UpdateProduct_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync((Product?)null);

        // Act
        var result = await _sut.UpdateProduct(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Product not found.");
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturnFailure_WhenDuplicateTitleExists()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries = [];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateProduct(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Another product with the same title already exists.");
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries = [];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.UpdateProduct(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to update product.");
    }

    [Fact]
    public async Task UpdateProduct_ShouldUpdateMainImage_WhenNewImageProvided()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        updateDto.MainImage = CreateImageDto("new-main-image");
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries = [];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockImageService.Setup(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("new-image.jpg");
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateProduct(updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockImageService.Verify(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    #endregion

    #endregion

    #region DeleteProduct Tests

    [Fact]
    public async Task DeleteProduct_ShouldReturnSuccess_WhenProductExists()
    {
        // Arrange
        var product = CreateProduct(1, "Test Product");
        product.Galleries = [];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mockImageService.Setup(s => s.DeleteImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteProduct(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockProductRepository.Verify(r => r.Delete(product), Times.Once);
        
        // Verify cache invalidation
        _mockCacheService.Verify(c => c.Remove("HomePage:Data"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Products:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Shop:GetAvailableProducts:"), Times.Once);
    }

    [Fact]
    public async Task DeleteProduct_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetProductByIdAsync(It.IsAny<int>())).ReturnsAsync((Product?)null);

        // Act
        var result = await _sut.DeleteProduct(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Product not found.");
    }

    [Fact]
    public async Task DeleteProduct_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var product = CreateProduct(1, "Test Product");

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.DeleteProduct(1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to delete product.");
    }

    [Fact]
    public async Task DeleteProduct_ShouldDeleteMainAndGalleryImages()
    {
        // Arrange
        var product = CreateProduct(1, "Test Product");
        product.Galleries =
        [
            new() { Id = 1, ImageName = "gallery1.jpg" },
            new() { Id = 2, ImageName = "gallery2.jpg" }
        ];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mockImageService.Setup(s => s.DeleteImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteProduct(1);

        // Assert - Main image + 2 gallery images = 3 delete calls
        _mockImageService.Verify(s => s.DeleteImageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public async Task UpdateProduct_ShouldInvalidateProductDetailsCache_WhenUpdateSucceeds()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        updateDto.Id = 5;
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries = [];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateProduct(updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCacheService.Verify(c => c.RemoveByPrefix("Products:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.Remove($"Products:GetById:{updateDto.Id}"), Times.Once);
        _mockCacheService.Verify(c => c.Remove("HomePage:Data"), Times.Once);
        _mockCacheService.Verify(c => c.Remove($"ProductDetails:GetById:{updateDto.Id}"), Times.Once);
    }

    [Fact]
    public async Task DeleteProduct_ShouldInvalidateProductDetailsCache_WhenDeleteSucceeds()
    {
        // Arrange
        int productId = 10;
        var product = CreateProduct(productId, "Test Product");

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mockImageService.Setup(s => s.DeleteImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteProduct(productId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockCacheService.Verify(c => c.RemoveByPrefix("Products:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.Remove($"Products:GetById:{productId}"), Times.Once);
        _mockCacheService.Verify(c => c.Remove("HomePage:Data"), Times.Once);
        _mockCacheService.Verify(c => c.Remove($"ProductDetails:GetById:{productId}"), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public async Task UpdateProduct_ShouldInvalidateCorrectProductDetailsCache_ForDifferentProductIds(int productId)
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        updateDto.Id = productId;
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries = [];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.UpdateProduct(updateDto);

        // Assert
        _mockCacheService.Verify(c => c.Remove($"ProductDetails:GetById:{productId}"), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(999)]
    public async Task DeleteProduct_ShouldInvalidateCorrectProductDetailsCache_ForDifferentProductIds(int productId)
    {
        // Arrange
        var product = CreateProduct(productId, "Test Product");

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mockImageService.Setup(s => s.DeleteImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteProduct(productId);

        // Assert
        _mockCacheService.Verify(c => c.Remove($"ProductDetails:GetById:{productId}"), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_ShouldNotInvalidateProductDetailsCache_WhenUpdateFails()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries = [];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0); // Fail

        // Act
        var result = await _sut.UpdateProduct(updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _mockCacheService.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProduct_ShouldNotInvalidateCache_WhenProductNotFound()
    {
        // Arrange
        int productId = 999;

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId)).ReturnsAsync((Product?)null);

        // Act
        var result = await _sut.DeleteProduct(productId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _mockCacheService.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProduct_ShouldNotInvalidateCache_WhenSaveChangesFails()
    {
        // Arrange
        var product = CreateProduct(1, "Test Product");

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0); // Fail

        // Act
        var result = await _sut.DeleteProduct(1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _mockCacheService.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region GetAllProductsPaginatedAsync Tests

    [Fact]
    public async Task GetAllProductsPaginatedAsync_ShouldReturnPaginatedResult()
    {
        // Arrange
        var parameters = new PaginationParameters { PageNumber = 1, PageSize = 10 };
        var products = new List<Product> { CreateProduct(1, "Product 1") };

        _mockProductRepository.Setup(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()))
            .ReturnsAsync(products);

        // Act
        var result = await _sut.GetAllProductsPaginatedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    #endregion

    #region AreAllProductsExistAsync Tests

    [Fact]
    public async Task AreAllProductsExistAsync_ShouldReturnTrue_WhenAllProductsExist()
    {
        // Arrange
        var productIds = new List<int> { 1, 2, 3 };

        _mockProductRepository.Setup(r => r.AreAllProductIdsExistAsync(productIds))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.AreAllProductsExistAsync(productIds);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AreAllProductsExistAsync_ShouldReturnFalse_WhenSomeProductsDontExist()
    {
        // Arrange
        var productIds = new List<int> { 1, 2, 999 };

        _mockProductRepository.Setup(r => r.AreAllProductIdsExistAsync(productIds))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.AreAllProductsExistAsync(productIds);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AreAllProductsExistAsync_ShouldReturnFalse_WhenProductIdsIsNull()
    {
        // Act
        var result = await _sut.AreAllProductsExistAsync(null!);

        // Assert
        result.Should().BeFalse();
        _mockProductRepository.Verify(r => r.AreAllProductIdsExistAsync(It.IsAny<List<int>>()), Times.Never);
    }

    [Fact]
    public async Task AreAllProductsExistAsync_ShouldReturnFalse_WhenProductIdsIsEmpty()
    {
        // Act
        var result = await _sut.AreAllProductsExistAsync([]);

        // Assert
        result.Should().BeFalse();
        _mockProductRepository.Verify(r => r.AreAllProductIdsExistAsync(It.IsAny<List<int>>()), Times.Never);
    }

    #endregion

    #region Caching Tests

    #region GetProductByIdAsync Caching Tests

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        var cachedProduct = new ProductDto { Id = 1, Title = "Cached Product" };
        
        _mockCacheService.Setup(c => c.Get<ProductDto>("Products:GetById:1"))
            .Returns(cachedProduct);

        // Act
        var result = await _sut.GetProductByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Title.Should().Be("Cached Product");
        
        // Verify repository was NOT called
        _mockProductRepository.Verify(r => r.GetProductByIdAsync(It.IsAny<int>()), Times.Never);
        
        // Verify cache was checked
        _mockCacheService.Verify(c => c.Get<ProductDto>("Products:GetById:1"), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldFetchFromRepository_WhenCacheMiss()
    {
        // Arrange
        var product = CreateProduct(1, "Test Product");
        
        _mockCacheService.Setup(c => c.Get<ProductDto>("Products:GetById:1"))
            .Returns((ProductDto?)null);
        _mockProductRepository.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);

        // Act
        var result = await _sut.GetProductByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Title.Should().Be("Test Product");
        
        // Verify repository was called
        _mockProductRepository.Verify(r => r.GetProductByIdAsync(1), Times.Once);
        
        // Verify cache was set with 15 minutes TTL
        _mockCacheService.Verify(c => c.Set("Products:GetById:1", 
            It.IsAny<ProductDto>(), 
            TimeSpan.FromMinutes(15)), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldNotCache_WhenProductNotFound()
    {
        // Arrange
        _mockCacheService.Setup(c => c.Get<ProductDto>("Products:GetById:999"))
            .Returns((ProductDto?)null);
        _mockProductRepository.Setup(r => r.GetProductByIdAsync(999)).ReturnsAsync((Product?)null);

        // Act
        var result = await _sut.GetProductByIdAsync(999);

        // Assert
        result.Should().BeNull();
        
        // Verify null result is NOT cached
        _mockCacheService.Verify(c => c.Set(It.IsAny<string>(), 
            It.IsAny<ProductDto>(), 
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999)]
    public async Task GetProductByIdAsync_ShouldUseCorrectCacheKey_ForDifferentProductIds(int productId)
    {
        // Arrange
        var product = CreateProduct(productId, "Test Product");
        var expectedCacheKey = $"Products:GetById:{productId}";
        
        _mockCacheService.Setup(c => c.Get<ProductDto>(expectedCacheKey))
            .Returns((ProductDto?)null);
        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId)).ReturnsAsync(product);

        // Act
        await _sut.GetProductByIdAsync(productId);

        // Assert
        _mockCacheService.Verify(c => c.Get<ProductDto>(expectedCacheKey), Times.Once);
        _mockCacheService.Verify(c => c.Set(expectedCacheKey, 
            It.IsAny<ProductDto>(), 
            TimeSpan.FromMinutes(15)), Times.Once);
    }

    #endregion

    #region GetAllProductsPaginatedAsync Caching Tests

    [Fact]
    public async Task GetAllProductsPaginatedAsync_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SortBy = "Title",
            SortOrder = "asc"
        };
        
        var cachedResult = new PaginatedResult<ProductDto>
        {
            Items = new List<ProductDto> { new() { Id = 1, Title = "Cached Product" } },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };
        
        var expectedCacheKey = "Products:Paginated:1:10:Title:asc";
        _mockCacheService.Setup(c => c.Get<PaginatedResult<ProductDto>>(expectedCacheKey))
            .Returns(cachedResult);

        // Act
        var result = await _sut.GetAllProductsPaginatedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        
        // Verify repository was NOT called
        _mockProductRepository.Verify(r => r.GetPagedAsync(It.IsAny<PaginationParameters>()), Times.Never);
        
        // Verify cache was checked
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ProductDto>>(expectedCacheKey), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsPaginatedAsync_ShouldFetchFromRepository_WhenCacheMiss()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            TotalCount = 2
        };
        
        var products = new List<Product>
        {
            CreateProduct(1, "Product 1"),
            CreateProduct(2, "Product 2")
        };
        
        _mockCacheService.Setup(c => c.Get<PaginatedResult<ProductDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ProductDto>?)null);
        _mockProductRepository.Setup(r => r.GetPagedAsync(parameters)).ReturnsAsync(products);

        // Act
        var result = await _sut.GetAllProductsPaginatedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
        
        // Verify repository was called
        _mockProductRepository.Verify(r => r.GetPagedAsync(parameters), Times.Once);
        
        // Verify cache was set with 3 minutes sliding expiration
        _mockCacheService.Verify(c => c.Set(It.IsAny<string>(), 
            It.IsAny<PaginatedResult<ProductDto>>(), 
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsPaginatedAsync_ShouldBuildCorrectCacheKey_WithoutSearchParameters()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 2, 
            PageSize = 25,
            SortBy = "Price",
            SortOrder = "desc"
        };
        
        // Cache key should NOT include search parameters
        var expectedCacheKey = "Products:Paginated:2:25:Price:desc";
        
        _mockCacheService.Setup(c => c.Get<PaginatedResult<ProductDto>>(expectedCacheKey))
            .Returns((PaginatedResult<ProductDto>?)null);
        _mockProductRepository.Setup(r => r.GetPagedAsync(parameters)).ReturnsAsync(new List<Product>());

        // Act
        await _sut.GetAllProductsPaginatedAsync(parameters);

        // Assert
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ProductDto>>(expectedCacheKey), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsPaginatedAsync_ShouldSkipCache_WhenSearchValueProvided()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SearchBy = "Title",
            SearchValue = "laptop",
            TotalCount = 5
        };
        
        var products = new List<Product> { CreateProduct(1, "Laptop") };
        _mockProductRepository.Setup(r => r.GetPagedAsync(parameters)).ReturnsAsync(products);

        // Act
        var result = await _sut.GetAllProductsPaginatedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        
        // Verify cache was NOT checked or set (search queries bypass cache)
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ProductDto>>(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(c => c.Set(It.IsAny<string>(), 
            It.IsAny<PaginatedResult<ProductDto>>(), 
            It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan>()), Times.Never);
        
        // Verify repository was called directly
        _mockProductRepository.Verify(r => r.GetPagedAsync(parameters), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsPaginatedAsync_ShouldNormalizeNullSortBy_InCacheKey()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SortBy = null,
            SortOrder = "asc"
        };
        
        // Expected: null SortBy should be normalized to "default"
        var expectedCacheKey = "Products:Paginated:1:10:default:asc";
        
        _mockCacheService.Setup(c => c.Get<PaginatedResult<ProductDto>>(expectedCacheKey))
            .Returns((PaginatedResult<ProductDto>?)null);
        _mockProductRepository.Setup(r => r.GetPagedAsync(parameters)).ReturnsAsync(new List<Product>());

        // Act
        await _sut.GetAllProductsPaginatedAsync(parameters);

        // Assert
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ProductDto>>(expectedCacheKey), Times.Once);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 25)]
    [InlineData(5, 50)]
    public async Task GetAllProductsPaginatedAsync_ShouldCreateDifferentCacheKeys_ForDifferentPageParameters(
        int pageNumber, int pageSize)
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = pageNumber, 
            PageSize = pageSize
        };
        
        _mockCacheService.Setup(c => c.Get<PaginatedResult<ProductDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ProductDto>?)null);
        _mockProductRepository.Setup(r => r.GetPagedAsync(parameters)).ReturnsAsync(new List<Product>());

        // Act
        await _sut.GetAllProductsPaginatedAsync(parameters);

        // Assert - verify a cache key was used (it will include the parameters)
        _mockCacheService.Verify(c => c.Get<PaginatedResult<ProductDto>>(It.IsAny<string>()), Times.Once);
        _mockCacheService.Verify(c => c.Set(It.IsAny<string>(), 
            It.IsAny<PaginatedResult<ProductDto>>(), 
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsPaginatedAsync_ShouldUseSlidingExpiration_ForCachedResults()
    {
        // Arrange
        var parameters = new PaginationParameters 
        { 
            PageNumber = 1, 
            PageSize = 10,
            SortBy = "Title",
            SortOrder = "asc"
        };
        
        var products = new List<Product> { CreateProduct(1, "Product 1") };
        
        _mockCacheService.Setup(c => c.Get<PaginatedResult<ProductDto>>(It.IsAny<string>()))
            .Returns((PaginatedResult<ProductDto>?)null);
        _mockProductRepository.Setup(r => r.GetPagedAsync(parameters)).ReturnsAsync(products);

        // Act
        await _sut.GetAllProductsPaginatedAsync(parameters);

        // Assert - Verify sliding expiration is used (3 minutes for both absolute and sliding)
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<PaginatedResult<ProductDto>>(), 
            TimeSpan.FromMinutes(3),  // absoluteExpiration
            TimeSpan.FromMinutes(3)), // slidingExpiration
            Times.Once);
    }

    #endregion

    #region Cache Invalidation Tests for Write Operations

    [Fact]
    public async Task CreateProduct_ShouldInvalidatePaginatedCache()
    {
        // Arrange
        var createProductDto = CreateValidCreateProductDto();

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockImageService.Setup(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("saved-image.jpg");
        _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.CreateProduct(createProductDto);

        // Assert
        _mockCacheService.Verify(c => c.RemoveByPrefix("Products:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.Remove("HomePage:Data"), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_ShouldInvalidateAllRelatedCaches_IncludingPaginated()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries =
        [
            new() { Id = 1, ImageName = "gallery1.jpg" }
        ];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.UpdateProduct(updateDto);

        // Assert - Verify all related caches are invalidated
        _mockCacheService.Verify(c => c.RemoveByPrefix("Products:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.Remove($"Products:GetById:{updateDto.Id}"), Times.Once);
        _mockCacheService.Verify(c => c.Remove("HomePage:Data"), Times.Once);
        _mockCacheService.Verify(c => c.Remove($"ProductDetails:GetById:{updateDto.Id}"), Times.Once);
    }

    [Fact]
    public async Task DeleteProduct_ShouldInvalidateAllRelatedCaches_IncludingPaginated()
    {
        // Arrange
        var productId = 1;
        var product = CreateProduct(productId, "Test Product");

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mockImageService.Setup(s => s.DeleteImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteProduct(productId);

        // Assert - Verify all related caches are invalidated
        _mockCacheService.Verify(c => c.RemoveByPrefix("Products:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.Remove($"Products:GetById:{productId}"), Times.Once);
        _mockCacheService.Verify(c => c.Remove("HomePage:Data"), Times.Once);
        _mockCacheService.Verify(c => c.Remove($"ProductDetails:GetById:{productId}"), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_ShouldInvalidateCorrectProductCache_ForSpecificProductId()
    {
        // Arrange
        var updateDto = CreateValidUpdateProductDto();
        updateDto.Id = 42; // Specific ID
        var existingProduct = CreateProduct(updateDto.Id, "Original Title");
        existingProduct.Galleries = [];

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(updateDto.Id)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.UpdateProduct(updateDto);

        // Assert
        _mockCacheService.Verify(c => c.Remove("Products:GetById:42"), Times.Once);
        _mockCacheService.Verify(c => c.Remove("ProductDetails:GetById:42"), Times.Once);
    }

    [Fact]
    public async Task DeleteProduct_ShouldInvalidateCorrectProductCache_ForSpecificProductId()
    {
        // Arrange
        var productId = 99;
        var product = CreateProduct(productId, "Test Product");

        _mockProductRepository.Setup(r => r.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mockImageService.Setup(s => s.DeleteImageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteProduct(productId);

        // Assert
        _mockCacheService.Verify(c => c.Remove("Products:GetById:99"), Times.Once);
        _mockCacheService.Verify(c => c.Remove("ProductDetails:GetById:99"), Times.Once);
    }

    #endregion

    #endregion

    #region Helper Methods

    private static CreateProductDto CreateValidCreateProductDto()
    {
        return new CreateProductDto
        {
            Title = "Test Product",
            Summary = "Test Summary",
            Description = "Test Description",
            Price = 99.99m,
            Discount = 10m,
            QTY = 100,
            Tags = "tag1,tag2",
            MainImage = CreateImageDto("main-image"),
            Gallaries = [CreateImageDto("gallery1")]
        };
    }

    private static UpdateProductDto CreateValidUpdateProductDto()
    {
        return new UpdateProductDto
        {
            Id = 1,
            Title = "Updated Product",
            Summary = "Updated Summary",
            Description = "Updated Description",
            Price = 149.99m,
            Discount = 20m,
            QTY = 50,
            Tags = "tag1,tag2,tag3"
        };
    }

    private static ImageDto CreateImageDto(string name)
    {
        return new ImageDto
        {
            Data = new byte[] { 1, 2, 3, 4, 5 },
            Type = "image/jpeg"
        };
    }

    private static Product CreateProduct(int id, string title)
    {
        return new Product
        {
            Id = id,
            Title = title,
            Summary = "Summary",
            Description = "Description",
            Price = 99.99m,
            QTY = 100,
            ImageName = "product.jpg",
            Galleries = []
        };
    }

    #endregion
}
