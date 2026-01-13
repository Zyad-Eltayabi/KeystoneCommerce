using KeystoneCommerce.Application.DTOs.Coupon;
using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.Product;
using KeystoneCommerce.Application.DTOs.ShippingDetails;
using KeystoneCommerce.Application.DTOs.ShippingMethod;
using KeystoneCommerce.Infrastructure.Validation.Validators.Order;
using KeystoneCommerce.Application.Common.Result_Pattern;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("OrderServiceTests")]
public class OrderServiceTest
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly IApplicationValidator<CreateOrderDto> _createOrderValidator;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<IProductService> _mockProductService;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ICouponService> _mockCouponService;
    private readonly Mock<IShippingAddressService> _mockShippingAddressService;
    private readonly Mock<IShippingMethodService> _mockShippingMethodService;
    private readonly IMappingService _mappingService;
    private readonly OrderService _sut;

    public OrderServiceTest()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<OrderService>>();
        
        var fluentValidator = new CreateOrderDtoValidator();
        _createOrderValidator = new FluentValidationAdapter<CreateOrderDto>(fluentValidator);
        
        _mockIdentityService = new Mock<IIdentityService>();
        _mockProductService = new Mock<IProductService>();
        _mockProductRepository =new Mock<IProductRepository>();
        _mockCouponService = new Mock<ICouponService>();
        _mockShippingAddressService = new Mock<IShippingAddressService>();
        _mockShippingMethodService = new Mock<IShippingMethodService>();
        _mappingService = new MappingService(MapperHelper.CreateMapper());

        _sut = new OrderService(
            _mockOrderRepository.Object,
            _mockLogger.Object,
            _createOrderValidator,
            _mockIdentityService.Object,
            _mockProductService.Object,
            _mockProductRepository.Object,
            _mockCouponService.Object,
            _mockShippingAddressService.Object,
            _mockShippingMethodService.Object,
            _mappingService);
    }

    #region CreateNewOrder Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CreateNewOrder_ShouldReturnSuccess_WhenValidOrderWithAllRequiredFields()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        SetupSuccessfulOrderCreation(createOrderDto);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.OrderNumber.Should().StartWith("Ord-");
        result.Data.Status.Should().Be(OrderStatus.Processing);
        result.Data.IsPaid.Should().BeFalse();
        result.Errors.Should().BeEmpty();

        VerifyAllMocksCalledOnce();
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnSuccess_WhenValidOrderWithCoupon()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.Coupon = "SUMMER20";
        
        SetupSuccessfulOrderCreationWithCoupon(createOrderDto, 20);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Discount.Should().BeGreaterThan(0);
        
        _mockCouponService.Verify(s => s.GetCouponByName("SUMMER20"), Times.Once);
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnSuccess_WhenValidOrderWithoutCoupon()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.Coupon = null;
        
        SetupSuccessfulOrderCreation(createOrderDto);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Discount.Should().Be(0);
        
        _mockCouponService.Verify(s => s.GetCouponByName(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 5)]
    [InlineData(5, 10)]
    public async Task CreateNewOrder_ShouldHandleDifferentProductQuantities_Successfully(int productCount, int quantity)
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity.Clear();
        
        for (int i = 1; i <= productCount; i++)
        {
            createOrderDto.ProductsWithQuantity.Add(i, quantity);
        }
        
        SetupSuccessfulOrderCreation(createOrderDto);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenShippingMethodIsEmpty()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ShippingMethod = "";

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Shipping method is required.");
        result.Data.Should().BeNull();

        _mockIdentityService.Verify(s => s.IsUserExistsById(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenUserIdIsEmpty()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.UserId = "";

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User ID is required.");
        _mockIdentityService.Verify(s => s.IsUserExistsById(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenProductsWithQuantityIsNull()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = null!;

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Products with quantity is required.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenProductsWithQuantityIsEmpty()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = new Dictionary<int, int>();

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("At least one product must be included in the order.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenShippingDetailsIsNull()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ShippingDetails = null!;

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Shipping details are required.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenProductIdIsInvalid()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = new Dictionary<int, int> { { 0, 1 } };

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Product ID must be greater than 0.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenProductQuantityIsZero()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = new Dictionary<int, int> { { 1, 0 } };

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Product quantity must be greater than 0.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenMultipleValidationErrorsOccur()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ShippingMethod = "";
        createOrderDto.UserId = "";
        createOrderDto.ProductsWithQuantity = new Dictionary<int, int>();

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        _mockIdentityService.Setup(s => s.IsUserExistsById(createOrderDto.UserId))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Please sign in first to complete your order.");
        
        _mockIdentityService.Verify(s => s.IsUserExistsById(createOrderDto.UserId), Times.Once);
        _mockCouponService.Verify(s => s.GetCouponByName(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenCouponIsInvalid()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.Coupon = "INVALID";
        
        _mockIdentityService.Setup(s => s.IsUserExistsById(It.IsAny<string>())).ReturnsAsync(true);
        _mockCouponService.Setup(s => s.GetCouponByName("INVALID"))
            .ReturnsAsync(Result<CouponDto>.Failure("Coupon not found."));

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid coupon code.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenShippingMethodIsInvalid()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        
        _mockIdentityService.Setup(s => s.IsUserExistsById(It.IsAny<string>())).ReturnsAsync(true);
        _mockCouponService.Setup(s => s.GetCouponByName(It.IsAny<string>()))
            .ReturnsAsync(Result<CouponDto>.Success(new CouponDto()));
        _mockShippingMethodService.Setup(s => s.GetShippingMethodByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((ShippingMethodDto?)null);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid shipping method.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenProductsDoNotExist()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        
        _mockIdentityService.Setup(s => s.IsUserExistsById(It.IsAny<string>())).ReturnsAsync(true);
        _mockCouponService.Setup(s => s.GetCouponByName(It.IsAny<string>()))
            .ReturnsAsync(Result<CouponDto>.Success(new CouponDto()));
        _mockShippingMethodService.Setup(s => s.GetShippingMethodByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateShippingMethod());
        _mockProductService.Setup(s => s.AreAllProductsExistAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("One or more products not found.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenShippingAddressCreationFails()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        
        _mockIdentityService.Setup(s => s.IsUserExistsById(It.IsAny<string>())).ReturnsAsync(true);
        _mockCouponService.Setup(s => s.GetCouponByName(It.IsAny<string>()))
            .ReturnsAsync(Result<CouponDto>.Success(new CouponDto()));
        _mockShippingMethodService.Setup(s => s.GetShippingMethodByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateShippingMethod());
        _mockProductService.Setup(s => s.AreAllProductsExistAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(true);
        _mockShippingAddressService.Setup(s => s.CreateNewAddress(It.IsAny<CreateShippingDetailsDto>()))
            .ReturnsAsync(Result<int>.Failure("Invalid address"));

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid shipping address details.");
    }

    [Fact]
    public async Task CreateNewOrder_ShouldReturnFailure_WhenStockReservationFails()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        
        _mockIdentityService.Setup(s => s.IsUserExistsById(It.IsAny<string>())).ReturnsAsync(true);
        _mockCouponService.Setup(s => s.GetCouponByName(It.IsAny<string>()))
            .ReturnsAsync(Result<CouponDto>.Success(new CouponDto()));
        _mockShippingMethodService.Setup(s => s.GetShippingMethodByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateShippingMethod());
        _mockProductService.Setup(s => s.AreAllProductsExistAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(true);
        _mockShippingAddressService.Setup(s => s.CreateNewAddress(It.IsAny<CreateShippingDetailsDto>()))
            .ReturnsAsync(Result<int>.Success(1));
        _mockProductRepository.Setup(r => r.DecreaseProductStock(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Insufficient stock"));

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Insufficient stock for one or more products.");
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task CreateNewOrder_ShouldCalculateSubTotalCorrectly()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = new Dictionary<int, int> { { 1, 2 } };
        
        _mockIdentityService.Setup(s => s.IsUserExistsById(createOrderDto.UserId)).ReturnsAsync(true);
        _mockCouponService.Setup(s => s.GetCouponByName(It.IsAny<string>()))
            .ReturnsAsync(Result<CouponDto>.Success(new CouponDto()));
        _mockShippingMethodService.Setup(s => s.GetShippingMethodByNameAsync(createOrderDto.ShippingMethod))
            .ReturnsAsync(CreateShippingMethod());
        _mockProductService.Setup(s => s.AreAllProductsExistAsync(It.IsAny<List<int>>())).ReturnsAsync(true);
        _mockShippingAddressService.Setup(s => s.CreateNewAddress(It.IsAny<CreateShippingDetailsDto>()))
            .ReturnsAsync(Result<int>.Success(1));
        _mockProductRepository.Setup(r => r.DecreaseProductStock(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        
        var productDetails = new List<ProductDetailsForOrderCreationDto>
        {
            new() { Id = 1, Title = "Product 1", Price = 100m, Discount = null }
        };
        
        _mockProductRepository.Setup(r => r.GetProductsForOrderCreationAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(productDetails);
        
        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(false);
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _mockOrderRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.Data!.SubTotal.Should().Be(200m); // 100 * 2
    }

    [Fact]
    public async Task CreateNewOrder_ShouldApplyProductDiscount_WhenProductHasDiscount()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = new Dictionary<int, int> { { 1, 1 } };
        
        _mockIdentityService.Setup(s => s.IsUserExistsById(createOrderDto.UserId)).ReturnsAsync(true);
        _mockCouponService.Setup(s => s.GetCouponByName(It.IsAny<string>()))
            .ReturnsAsync(Result<CouponDto>.Success(new CouponDto()));
        _mockShippingMethodService.Setup(s => s.GetShippingMethodByNameAsync(createOrderDto.ShippingMethod))
            .ReturnsAsync(CreateShippingMethod());
        _mockProductService.Setup(s => s.AreAllProductsExistAsync(It.IsAny<List<int>>())).ReturnsAsync(true);
        _mockShippingAddressService.Setup(s => s.CreateNewAddress(It.IsAny<CreateShippingDetailsDto>()))
            .ReturnsAsync(Result<int>.Success(1));
        _mockProductRepository.Setup(r => r.DecreaseProductStock(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        
        var productDetails = new List<ProductDetailsForOrderCreationDto>
        {
            new() { Id = 1, Title = "Product 1", Price = 100m, Discount = 20m }
        };
        
        _mockProductRepository.Setup(r => r.GetProductsForOrderCreationAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(productDetails);
        
        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(false);
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _mockOrderRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.Data!.SubTotal.Should().Be(80m); // 100 - 20
    }

    [Fact]
    public async Task CreateNewOrder_ShouldCalculateTotalWithShippingCost()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        
        SetupSuccessfulOrderCreation(createOrderDto, shippingPrice: 10m);

        // Act
        var result = await _sut.CreateNewOrder(createOrderDto);

        // Assert
        result.Data!.Total.Should().BeGreaterThan(result.Data.SubTotal);
        result.Data.Shipping.Should().Be(10m);
    }

    #endregion

    #endregion

    #region MarkOrderAsPaid Tests

    [Fact]
    public async Task MarkOrderAsPaid_ShouldReturnSuccess_WhenOrderExistsAndNotPaid()
    {
        // Arrange
        int orderId = 1;
        var order = CreateUnpaidOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockOrderRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.MarkOrderAsPaid(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.IsPaid.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        
        _mockOrderRepository.Verify(r => r.Update(order), Times.Once);
        _mockOrderRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkOrderAsPaid_ShouldReturnFailure_WhenOrderNotFound()
    {
        // Arrange
        int orderId = 999;
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        var result = await _sut.MarkOrderAsPaid(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Order not found.");
        
        _mockOrderRepository.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task MarkOrderAsPaid_ShouldReturnFailure_WhenOrderAlreadyPaid()
    {
        // Arrange
        int orderId = 1;
        var order = CreatePaidOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _sut.MarkOrderAsPaid(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Order is already paid.");
        
        _mockOrderRepository.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task MarkOrderAsPaid_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        int orderId = 1;
        var order = CreateUnpaidOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockOrderRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.MarkOrderAsPaid(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to update order payment status.");
    }

    #endregion

    #region UpdateOrderStatusToFailed Tests

    [Fact]
    public async Task UpdateOrderStatusToFailed_ShouldReturnSuccess_WhenOrderExistsAndCanBeFailed()
    {
        // Arrange
        int orderId = 1;
        var order = CreateUnpaidOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockOrderRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateOrderStatusToFailed(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Failed);
        
        _mockOrderRepository.Verify(r => r.Update(order), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusToFailed_ShouldReturnFailure_WhenOrderNotFound()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);

        // Act
        var result = await _sut.UpdateOrderStatusToFailed(1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Order not found.");
    }

    [Fact]
    public async Task UpdateOrderStatusToFailed_ShouldReturnFailure_WhenOrderAlreadyPaid()
    {
        // Arrange
        int orderId = 1;
        var order = CreatePaidOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _sut.UpdateOrderStatusToFailed(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot mark a paid order as failed.");
    }

    [Fact]
    public async Task UpdateOrderStatusToFailed_ShouldReturnFailure_WhenOrderAlreadyCancelled()
    {
        // Arrange
        int orderId = 1;
        var order = CreateCancelledOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _sut.UpdateOrderStatusToFailed(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot mark a cancelled order as failed.");
    }

    #endregion

    #region UpdateOrderStatusToCancelled Tests

    [Fact]
    public async Task UpdateOrderStatusToCancelled_ShouldReturnSuccess_WhenOrderCanBeCancelled()
    {
        // Arrange
        int orderId = 1;
        var order = CreateUnpaidOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _mockOrderRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateOrderStatusToCancelled(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateOrderStatusToCancelled_ShouldReturnFailure_WhenOrderAlreadyPaid()
    {
        // Arrange
        int orderId = 1;
        var order = CreatePaidOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _sut.UpdateOrderStatusToCancelled(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot cancel a paid order.");
    }

    [Fact]
    public async Task UpdateOrderStatusToCancelled_ShouldReturnFailure_WhenOrderAlreadyCancelled()
    {
        // Arrange
        int orderId = 1;
        var order = CreateCancelledOrder(orderId);
        
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _sut.UpdateOrderStatusToCancelled(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Order is already cancelled.");
    }

    #endregion

    #region ReleaseReservedStock Tests

    [Fact]
    public async Task ReleaseReservedStock_ShouldReturnTrue_WhenStockReleasedSuccessfully()
    {
        // Arrange
        int orderId = 1;
        _mockOrderRepository.Setup(r => r.ReleaseReservedStock(orderId)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ReleaseReservedStock(orderId);

        // Assert
        result.Should().BeTrue();
        _mockOrderRepository.Verify(r => r.ReleaseReservedStock(orderId), Times.Once);
    }

    [Fact]
    public async Task ReleaseReservedStock_ShouldReturnFalse_WhenExceptionOccurs()
    {
        // Arrange
        int orderId = 1;
        _mockOrderRepository.Setup(r => r.ReleaseReservedStock(orderId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.ReleaseReservedStock(orderId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static CreateOrderDto CreateValidOrderDto()
    {
        return new CreateOrderDto
        {
            ShippingMethod = "Standard",
            Coupon = null,
            UserId = "user-123",
            PaymentProvider = "Stripe",
            ShippingDetails = new CreateShippingDetailsDto
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Address = "123 Main St",
                City = "New York",
                Country = "USA",
                Phone = "+1234567890",
                PostalCode = "10001"
            },
            ProductsWithQuantity = new Dictionary<int, int> { { 1, 2 }, { 2, 1 } }
        };
    }

    private static ShippingMethodDto CreateShippingMethod(decimal price = 10m)
    {
        return new ShippingMethodDto
        {
            Id = 1,
            Name = "Standard",
            Description = "Standard Shipping",
            Price = price,
            EstimatedDays = "5-7 days"
        };
    }

    private static Order CreateUnpaidOrder(int id)
    {
        return new Order
        {
            Id = id,
            OrderNumber = $"Ord-{Guid.NewGuid().ToString()[..6]}",
            UserId = "user-123",
            SubTotal = 100m,
            Total = 110m,
            Shipping = 10m,
            Discount = 0m,
            IsPaid = false,
            Status = OrderStatus.Processing
        };
    }

    private static Order CreatePaidOrder(int id)
    {
        var order = CreateUnpaidOrder(id);
        order.IsPaid = true;
        order.Status = OrderStatus.Paid;
        return order;
    }

    private static Order CreateCancelledOrder(int id)
    {
        var order = CreateUnpaidOrder(id);
        order.Status = OrderStatus.Cancelled;
        return order;
    }

    private void SetupSuccessfulOrderCreation(CreateOrderDto createOrderDto, decimal shippingPrice = 10m)
    {
        _mockIdentityService.Setup(s => s.IsUserExistsById(createOrderDto.UserId)).ReturnsAsync(true);
        
        _mockCouponService.Setup(s => s.GetCouponByName(It.IsAny<string>()))
            .ReturnsAsync(Result<CouponDto>.Success(new CouponDto()));
        
        _mockShippingMethodService.Setup(s => s.GetShippingMethodByNameAsync(createOrderDto.ShippingMethod))
            .ReturnsAsync(CreateShippingMethod(shippingPrice));
        
        _mockProductService.Setup(s => s.AreAllProductsExistAsync(It.IsAny<List<int>>())).ReturnsAsync(true);
        
        _mockShippingAddressService.Setup(s => s.CreateNewAddress(It.IsAny<CreateShippingDetailsDto>()))
            .ReturnsAsync(Result<int>.Success(1));
        
        _mockProductRepository.Setup(r => r.DecreaseProductStock(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        
        // Dynamic product creation based on requested IDs
        _mockProductRepository.Setup(r => r.GetProductsForOrderCreationAsync(It.IsAny<List<int>>()))
            .ReturnsAsync((List<int> productIds) =>
            {
                return productIds.Select(id => new ProductDetailsForOrderCreationDto
                {
                    Id = id,
                    Title = $"Product {id}",
                    Price = 50m,
                    Discount = null
                }).ToList();
            });
        
        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(false);
        
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _mockOrderRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
    }

    private void SetupSuccessfulOrderCreationWithCoupon(CreateOrderDto createOrderDto, int discountPercentage)
    {
        _mockIdentityService.Setup(s => s.IsUserExistsById(createOrderDto.UserId)).ReturnsAsync(true);
        
        _mockCouponService.Setup(s => s.GetCouponByName(createOrderDto.Coupon!))
            .ReturnsAsync(Result<CouponDto>.Success(new CouponDto 
            { 
                Id = 1, 
                Code = createOrderDto.Coupon!, 
                DiscountPercentage = discountPercentage 
            }));
        
        _mockShippingMethodService.Setup(s => s.GetShippingMethodByNameAsync(createOrderDto.ShippingMethod))
            .ReturnsAsync(CreateShippingMethod());
        
        _mockProductService.Setup(s => s.AreAllProductsExistAsync(It.IsAny<List<int>>())).ReturnsAsync(true);
        
        _mockShippingAddressService.Setup(s => s.CreateNewAddress(It.IsAny<CreateShippingDetailsDto>()))
            .ReturnsAsync(Result<int>.Success(1));
        
        _mockProductRepository.Setup(r => r.DecreaseProductStock(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        
        // Dynamic product creation based on requested IDs
        _mockProductRepository.Setup(r => r.GetProductsForOrderCreationAsync(It.IsAny<List<int>>()))
            .ReturnsAsync((List<int> productIds) =>
            {
                return productIds.Select(id => new ProductDetailsForOrderCreationDto
                {
                    Id = id,
                    Title = $"Product {id}",
                    Price = 100m,
                    Discount = null
                }).ToList();
            });
        
        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(false);
        
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _mockOrderRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
    }

    private void VerifyAllMocksCalledOnce()
    {
        _mockIdentityService.Verify(s => s.IsUserExistsById(It.IsAny<string>()), Times.Once);
        _mockShippingMethodService.Verify(s => s.GetShippingMethodByNameAsync(It.IsAny<string>()), Times.Once);
        _mockProductService.Verify(s => s.AreAllProductsExistAsync(It.IsAny<List<int>>()), Times.Once);
        _mockShippingAddressService.Verify(s => s.CreateNewAddress(It.IsAny<CreateShippingDetailsDto>()), Times.Once);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion
}
