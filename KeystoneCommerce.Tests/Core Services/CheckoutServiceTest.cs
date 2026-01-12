using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.DTOs.ShippingDetails;
using KeystoneCommerce.Application.Common.Result_Pattern;
using Xunit;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("CheckoutServiceTests")]
public class CheckoutServiceTest
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CheckoutService>> _mockLogger;
    private readonly Mock<IInventoryReservationService> _mockInventoryReservationService;
    private readonly CheckoutService _sut;

    public CheckoutServiceTest()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockPaymentService = new Mock<IPaymentService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CheckoutService>>();
        _mockInventoryReservationService = new Mock<IInventoryReservationService>();

        _sut = new CheckoutService(
            _mockOrderService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockPaymentService.Object,
            _mockInventoryReservationService.Object);
    }

    #region SubmitOrder Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task SubmitOrder_ShouldReturnSuccess_WhenValidStripeOrder()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));

        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));

        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));

        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Data.Should().NotBeNull();
        result.Data!.PaymentId.Should().Be(100);
        result.Data.Id.Should().Be(orderDto.Id);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockOrderService.Verify(s => s.CreateNewOrder(createOrderDto), Times.Once);
        _mockInventoryReservationService.Verify(s => s.CreateReservationAsync(orderDto.Id, PaymentType.Stripe), Times.Once);
        _mockPaymentService.Verify(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldReturnSuccess_WhenValidCashOnDeliveryOrder()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.PaymentProvider = "CashOnDelivery";
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));

        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));

        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(200));

        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.PaymentId.Should().Be(200);

        _mockInventoryReservationService.Verify(s => s.CreateReservationAsync(orderDto.Id, PaymentType.CashOnDelivery), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task SubmitOrder_ShouldHandleDifferentProductQuantities_Successfully(int productCount)
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity.Clear();
        for (int i = 1; i <= productCount; i++)
        {
            createOrderDto.ProductsWithQuantity.Add(i, i * 2);
        }
        
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitOrder_ShouldCreatePaymentDtoWithCorrectProperties()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();
        orderDto.Total = 150.50m;
        orderDto.Currency = "USD";
        CreatePaymentDto? capturedPaymentDto = null;

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .Callback<CreatePaymentDto>(dto => capturedPaymentDto = dto)
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        capturedPaymentDto.Should().NotBeNull();
        capturedPaymentDto!.OrderId.Should().Be(orderDto.Id);
        capturedPaymentDto.Amount.Should().Be(orderDto.Total);
        capturedPaymentDto.Provider.Should().Be(PaymentType.Stripe);
        capturedPaymentDto.UserId.Should().Be(createOrderDto.UserId);
        capturedPaymentDto.Currency.Should().Be(orderDto.Currency);
        capturedPaymentDto.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public async Task SubmitOrder_ShouldSetPaymentIdInOrderDto_OnSuccess()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();
        int expectedPaymentId = 12345;

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(expectedPaymentId));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.Data!.PaymentId.Should().Be(expectedPaymentId);
    }

    [Fact]
    public async Task SubmitOrder_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();
        var callOrder = new List<string>();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
            .Callback(() => callOrder.Add("BeginTransaction"))
            .Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .Callback(() => callOrder.Add("CreateOrder"))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));

        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .Callback(() => callOrder.Add("CreateReservation"))
            .ReturnsAsync(Result<string>.Success("Reservation created"));

        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .Callback(() => callOrder.Add("CreatePayment"))
            .ReturnsAsync(Result<int>.Success(100));

        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Callback(() => callOrder.Add("Commit"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        callOrder.Should().Equal("BeginTransaction", "CreateOrder", "CreateReservation", "CreatePayment", "Commit");
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task SubmitOrder_ShouldReturnFailure_WhenProductsWithQuantityIsNull()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = null!;

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("The order must contain at least one product.");
        result.Data.Should().BeNull();

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _mockOrderService.Verify(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldReturnFailure_WhenProductsWithQuantityIsEmpty()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = new Dictionary<int, int>();

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("The order must contain at least one product.");

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _mockOrderService.Verify(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()), Times.Never);
    }

    [Theory]
    [InlineData("InvalidPaymentType")]
    [InlineData("stripe")] // lowercase - parseable but not defined
    [InlineData("STRIPE")] // uppercase - parseable but not defined
    [InlineData("PayPal")]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("3")] // Valid integer but not defined in enum
    [InlineData("-1")] // Negative number
    public async Task SubmitOrder_ShouldReturnFailure_WhenPaymentProviderIsInvalid(string invalidProvider)
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.PaymentProvider = invalidProvider;

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Invalid payment type.");
        result.Data.Should().BeNull();

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _mockOrderService.Verify(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldReturnFailure_WhenPaymentProviderIsNull()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.PaymentProvider = null!;

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid payment type.");
    }

    [Fact]
    public async Task SubmitOrder_ShouldReturnFailure_WhenPaymentProviderIsWhitespace()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.PaymentProvider = "   ";

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid payment type.");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task SubmitOrder_ShouldRollback_WhenOrderCreationFails()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Failure("Order creation failed"));

        _mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Order creation failed");
        result.Data.Should().BeNull();

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockOrderService.Verify(s => s.CreateNewOrder(createOrderDto), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockInventoryReservationService.Verify(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()), Times.Never);
        _mockPaymentService.Verify(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldRollback_WhenInventoryReservationFails()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));

        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Failure("Inventory reservation failed"));

        _mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Inventory reservation failed");

        _mockOrderService.Verify(s => s.CreateNewOrder(createOrderDto), Times.Once);
        _mockInventoryReservationService.Verify(s => s.CreateReservationAsync(orderDto.Id, PaymentType.Stripe), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockPaymentService.Verify(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldRollback_WhenPaymentCreationFails()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));

        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));

        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Failure("Payment creation failed"));

        _mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment creation failed");

        _mockOrderService.Verify(s => s.CreateNewOrder(createOrderDto), Times.Once);
        _mockInventoryReservationService.Verify(s => s.CreateReservationAsync(orderDto.Id, PaymentType.Stripe), Times.Once);
        _mockPaymentService.Verify(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldReturnMultipleErrors_WhenOrderCreationFailsWithMultipleErrors()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Failure(errors));

        _mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
        result.Errors.Should().Contain("Error 3");
    }

    [Fact]
    public async Task SubmitOrder_ShouldValidateInputs_BeforeBeginningTransaction()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = null!;

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldValidatePaymentType_BeforeBeginningTransaction()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.PaymentProvider = "InvalidType";

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    #endregion

    #region Error Handling Scenarios

    [Fact]
    public async Task SubmitOrder_ShouldHandleException_AndRollback()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        _mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("An unexpected error occurred while processing your order. Please try again later.");
        result.Data.Should().BeNull();

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldHandleNullReferenceException_Gracefully()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ThrowsAsync(new NullReferenceException("Object reference not set"));

        _mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("An unexpected error occurred while processing your order. Please try again later.");
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitOrder_ShouldHandleInvalidOperationException_Gracefully()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));

        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ThrowsAsync(new InvalidOperationException("Invalid operation"));

        _mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("An unexpected error occurred while processing your order. Please try again later.");
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task SubmitOrder_ShouldNotCommit_WhenExceptionOccurs()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ThrowsAsync(new Exception("Test exception"));

        _mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    #endregion

    #region Transaction Management Tests

    [Fact]
    public async Task SubmitOrder_ShouldBeginTransaction_BeforeAnyOperation()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var callOrder = new List<string>();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
            .Callback(() => callOrder.Add("BeginTransaction"))
            .Returns(Task.CompletedTask);
        
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .Callback(() => callOrder.Add("CreateOrder"))
            .ThrowsAsync(new Exception("Test"));

        _mockUnitOfWork.Setup(u => u.RollbackAsync())
            .Callback(() => callOrder.Add("Rollback"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        callOrder[0].Should().Be("BeginTransaction");
    }

    [Fact]
    public async Task SubmitOrder_ShouldCommitTransaction_OnlyWhenAllOperationsSucceed()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitOrder_ShouldNotRollback_WhenAllOperationsSucceed()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SubmitOrder_ShouldHandleCouponWithValue()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.Coupon = "SUMMER2024";
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitOrder_ShouldHandleCouponWithNullValue()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.Coupon = null;
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitOrder_ShouldHandleVeryLargeOrderTotal()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();
        orderDto.Total = decimal.MaxValue;
        CreatePaymentDto? capturedPaymentDto = null;

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .Callback<CreatePaymentDto>(dto => capturedPaymentDto = dto)
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedPaymentDto!.Amount.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public async Task SubmitOrder_ShouldHandleSpecialCharactersInShippingMethod()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ShippingMethod = "Express <>&\"' Shipping";
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitOrder_ShouldHandleUnicodeCharactersInUserId()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.UserId = "User-???-???????";
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(100, 999)]
    [InlineData(int.MaxValue - 1, int.MaxValue)]
    public async Task SubmitOrder_ShouldHandleProductIdAndQuantityBoundaries(int productId, int quantity)
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        createOrderDto.ProductsWithQuantity = new Dictionary<int, int> { { productId, quantity } };
        var orderDto = CreateValidOrderDtoResult();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task SubmitOrder_ShouldPassCorrectOrderToCreateReservation()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();
        orderDto.Id = 12345;

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(100));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        await _sut.SubmitOrder(createOrderDto);

        // Assert
        _mockInventoryReservationService.Verify(s => s.CreateReservationAsync(12345, PaymentType.Stripe), Times.Once);
    }

    [Fact]
    public async Task SubmitOrder_ShouldReturnOriginalOrderData_WithPaymentId()
    {
        // Arrange
        var createOrderDto = CreateValidOrderDto();
        var orderDto = CreateValidOrderDtoResult();
        orderDto.Id = 999;
        orderDto.OrderNumber = "ORD-123456";
        orderDto.Total = 250.75m;

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockOrderService.Setup(s => s.CreateNewOrder(It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(Result<OrderDto>.Success(orderDto));
        _mockInventoryReservationService.Setup(s => s.CreateReservationAsync(It.IsAny<int>(), It.IsAny<PaymentType>()))
            .ReturnsAsync(Result<string>.Success("Reservation created"));
        _mockPaymentService.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(Result<int>.Success(555));
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitOrder(createOrderDto);

        // Assert
        result.Data!.Id.Should().Be(999);
        result.Data.OrderNumber.Should().Be("ORD-123456");
        result.Data.Total.Should().Be(250.75m);
        result.Data.PaymentId.Should().Be(555);
    }

    #endregion

    #endregion

    #region Helper Methods

    private static CreateOrderDto CreateValidOrderDto()
    {
        return new CreateOrderDto
        {
            ShippingMethod = "Standard",
            Coupon = "SUMMER2024",
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
            ProductsWithQuantity = new Dictionary<int, int>
            {
                { 1, 2 },
                { 2, 1 }
            }
        };
    }

    private static OrderDto CreateValidOrderDtoResult()
    {
        return new OrderDto
        {
            Id = 1,
            OrderNumber = "ORD-ABC123",
            Status = OrderStatus.Processing,
            SubTotal = 100.00m,
            Total = 120.00m,
            Shipping = 20.00m,
            Discount = 0.00m,
            Currency = "USD",
            IsPaid = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
