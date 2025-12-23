using KeystoneCommerce.Application.DTOs.ShippingMethod;
namespace KeystoneCommerce.Tests.Core_Services;

[Collection("ShippingMethodServiceTests")]
public class ShippingMethodServiceTest
{
    private readonly Mock<IShippingMethodRepository> _repository;
    private readonly MappingService _mappingService;
    private readonly ShippingMethodService _shippingService;

    public ShippingMethodServiceTest()
    {
        _repository = new();
        _mappingService = new(MapperHelper.CreateMapper());
        _shippingService = new(_repository.Object, _mappingService);
    }

    #region GetAllShippingMethodsAsync

    [Fact]
    public async Task GetAllShippingMethodsAsync_ShouldReturnListOfShippingMethodDto_WhenShippingMethodsExist()
    {
        // Arrange
        var shippingMethods = new List<ShippingMethod>
        {
            new()
            {
                Id = 1, Name = "Standard", Description = "Standard Shipping", Price = 5.00m,
                EstimatedDays = "5-7 days"
            },
            new()
            {
                Id = 2, Name = "Express", Description = "Express Shipping", Price = 15.00m,
                EstimatedDays = "1-2 days"
            }
        };

        _repository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(shippingMethods);

        // Act
        var result = await _shippingService.GetAllShippingMethodsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(shippingMethods, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetAllShippingMethodsAsync_ShouldReturnEmptyList_WhenNoShippingMethodsExist()
    {
        // Arrange
        _repository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ShippingMethod>());

        // Act
        var result = await _shippingService.GetAllShippingMethodsAsync();

        // Assert
        result.Should().BeEmpty();
        result.Should().NotBeNull();
    }

    #endregion

    #region GetShippingMethodByNameAsync

    [Fact]
    public async Task GetShippingMethodByNameAsync_ShouldReturnShippingMethodDto_WhenShippingMethodExists()
    {
        // Arrange
        var entity = new ShippingMethod
        {
            Id = 1,
            Name = "Standard",
            Description = "Standard Shipping",
            Price = 5.00m,
            EstimatedDays = "5-7 days"
        };

        _repository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _shippingService.GetShippingMethodByNameAsync("Standard");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new ShippingMethodDto
        {
            Id = 1,
            Name = "Standard",
            Description = "Standard Shipping",
            Price = 5.00m,
            EstimatedDays = "5-7 days"
        });

        _repository.Verify(
            r => r.FindAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetShippingMethodByNameAsync_ShouldReturnNull_WhenShippingMethodDoesNotExist()
    {
        // Arrange
        _repository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()))
            .ReturnsAsync((ShippingMethod?)null);

        // Act
        var result = await _shippingService.GetShippingMethodByNameAsync("Standard");

        // Assert
        result.Should().BeNull();

        _repository.Verify(
            r => r.FindAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()),
            Times.Once);
    }


    [Theory]
    [MemberData(nameof(TestData.InvalidStrings), MemberType = typeof(TestData))]
    public async Task GetShippingMethodByNameAsync_ShouldReturnNull_WhenNameIsNullOrWhitespace(string shippingMethodName)
    {
        // Arrange
        var resultNull = await _shippingService.GetShippingMethodByNameAsync(shippingMethodName);

        // Act
        resultNull.Should().BeNull();

        // Assert
        _repository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<ShippingMethod, bool>>>()),
     Times.Never);
    }

    #endregion
}


/*
 Refactor this service method to follow these rules:
1. Change return type from `Task<List<T>?>` to `Task<List<T>>` (remove nullable)
2. Remove null check for repository result (GetAllAsync never returns null)
3. If no items exist, return empty list instead of null
4. Keep the empty check using `!items.Any()`
5. Maintain the same mapping logic

Example:
Before:
public async Task<List<FooDto>?> GetAllAsync()
{
    var items = await repository.GetAllAsync();
    if (items is null || !items.Any())
        return null;
    return mapper.Map<List<FooDto>>(items);
}

After:
public async Task<List<FooDto>> GetAllAsync()
{
    var items = await repository.GetAllAsync();
    if (!items.Any())
        return new List<FooDto>();
    return mapper.Map<List<FooDto>>(items);
}
 */