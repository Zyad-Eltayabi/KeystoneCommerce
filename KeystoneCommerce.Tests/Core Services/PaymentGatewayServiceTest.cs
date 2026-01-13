using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Notifications.Contracts;
using KeystoneCommerce.Application.Common.Result_Pattern;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("PaymentGatewayServiceTests")]
public class PaymentGatewayServiceTest
{
    private readonly Mock<IStripPaymentService> _mockStripPaymentService;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<IPaymentRepository> _mockPaymentRepository;
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PaymentGatewayService>> _mockLogger;
    private readonly Mock<IInventoryReservationService> _mockInventoryReservationService;
    private readonly Mock<INotificationOrchestrator> _mockNotificationOrchestrator;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IBackgroundService> _mockBackgroundService;
    private readonly PaymentGatewayService _sut;

    public PaymentGatewayServiceTest()
    {
        _mockStripPaymentService = new Mock<IStripPaymentService>();
        _mockPaymentService = new Mock<IPaymentService>();
        _mockPaymentRepository = new Mock<IPaymentRepository>();
        _mockOrderService = new Mock<IOrderService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PaymentGatewayService>>();
        _mockInventoryReservationService = new Mock<IInventoryReservationService>();
        _mockNotificationOrchestrator = new Mock<INotificationOrchestrator>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockBackgroundService = new Mock<IBackgroundService>();

        _sut = new PaymentGatewayService(
            _mockStripPaymentService.Object,
            _mockPaymentService.Object,
            _mockPaymentRepository.Object,
            _mockOrderService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockInventoryReservationService.Object,
            _mockNotificationOrchestrator.Object,
            _mockOrderRepository.Object,
            _mockBackgroundService.Object);
    }

    #region CreatePaymentSessionAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CreatePaymentSessionAsync_ShouldReturnSuccess_WhenSessionCreatedSuccessfully()
    {
        // Arrange
        var sessionDto = CreateValidSessionDto();
        var expectedResult = new PaymentSessionResultDto
        {
            SessionId = "session_123",
            PaymentUrl = "https://checkout.stripe.com/session_123",
            PaymentId = 1
        };

        _mockStripPaymentService.Setup(s => s.CreatePaymentSessionAsync(sessionDto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.CreatePaymentSessionAsync(sessionDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.SessionId.Should().Be("session_123");
        result.Data.PaymentUrl.Should().Be("https://checkout.stripe.com/session_123");
        result.Data.PaymentId.Should().Be(1);
        result.Errors.Should().BeEmpty();

        _mockStripPaymentService.Verify(s => s.CreatePaymentSessionAsync(sessionDto), Times.Once);
    }

    [Theory]
    [InlineData(1, 10.50)]
    [InlineData(100, 999.99)]
    [InlineData(5, 0.01)]
    public async Task CreatePaymentSessionAsync_ShouldHandleDifferentPrices_Successfully(int paymentId, decimal totalPrice)
    {
        // Arrange
        var sessionDto = new CreatePaymentSessionDto
        {
            PaymentId = paymentId,
            TotalPrice = totalPrice,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        var expectedResult = new PaymentSessionResultDto
        {
            SessionId = $"session_{paymentId}",
            PaymentUrl = $"https://checkout.stripe.com/session_{paymentId}",
            PaymentId = paymentId
        };

        _mockStripPaymentService.Setup(s => s.CreatePaymentSessionAsync(It.IsAny<CreatePaymentSessionDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.CreatePaymentSessionAsync(sessionDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.PaymentId.Should().Be(paymentId);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task CreatePaymentSessionAsync_ShouldReturnFailure_WhenStripeServiceThrowsException()
    {
        // Arrange
        var sessionDto = CreateValidSessionDto();

        _mockStripPaymentService.Setup(s => s.CreatePaymentSessionAsync(sessionDto))
            .ThrowsAsync(new Exception("Stripe API error"));

        // Act
        var result = await _sut.CreatePaymentSessionAsync(sessionDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to create payment session.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreatePaymentSessionAsync_ShouldReturnFailure_WhenStripeServiceThrowsStripeException()
    {
        // Arrange
        var sessionDto = CreateValidSessionDto();

        _mockStripPaymentService.Setup(s => s.CreatePaymentSessionAsync(sessionDto))
            .ThrowsAsync(new InvalidOperationException("Invalid API key"));

        // Act
        var result = await _sut.CreatePaymentSessionAsync(sessionDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to create payment session.");
    }

    #endregion

    #endregion

    #region ConfirmPaymentAndUpdateOrderAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldReturnSuccess_WhenAllStepsSucceed()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();
        SetupSuccessfulPaymentConfirmation(confirmDto);

        // Act
        var result = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        VerifyAllConfirmationStepsCalled();
    }

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldReturnSuccess_WhenPaymentAlreadyFulfilled()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _mockPaymentService.Verify(s => s.ConfirmPaymentAsync(It.IsAny<ConfirmPaymentDto>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldCallStepsInCorrectOrder()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();
        var callOrder = new List<string>();

        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(It.IsAny<int>()))
            .Callback(() => callOrder.Add("CheckFulfilled"))
            .ReturnsAsync(false);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
            .Callback(() => callOrder.Add("BeginTransaction"))
            .Returns(Task.CompletedTask);

        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(It.IsAny<ConfirmPaymentDto>()))
            .Callback(() => callOrder.Add("ConfirmPayment"))
            .ReturnsAsync(Result<bool>.Success());

        _mockPaymentRepository.Setup(r => r.GetOrderIdByPaymentIdAsync(It.IsAny<int>()))
            .Callback(() => callOrder.Add("GetOrderId"))
            .ReturnsAsync(1);

        _mockOrderService.Setup(s => s.MarkOrderAsPaid(It.IsAny<int>()))
            .Callback(() => callOrder.Add("MarkOrderAsPaid"))
            .ReturnsAsync(Result<bool>.Success());

        _mockInventoryReservationService.Setup(s => s.UpdateReservationStatusToConsumedAsync(It.IsAny<int>()))
            .Callback(() => callOrder.Add("UpdateReservation"))
            .ReturnsAsync(Result<string>.Success());

        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Callback(() => callOrder.Add("Commit"))
            .Returns(Task.CompletedTask);

        _mockBackgroundService.Setup(s => s.EnqueueJob<IPaymentGatewayService>(It.IsAny<Expression<Action<IPaymentGatewayService>>>()))
            .Callback(() => callOrder.Add("EnqueueEmail"));

        // Act
        await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        callOrder.Should().Equal("CheckFulfilled", "BeginTransaction", "ConfirmPayment", 
            "GetOrderId", "MarkOrderAsPaid", "UpdateReservation", "Commit", "EnqueueEmail");
    }

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldEnqueueEmailJob_AfterSuccessfulConfirmation()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();
        SetupSuccessfulPaymentConfirmation(confirmDto);

        // Act
        await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        _mockBackgroundService.Verify(s => s.EnqueueJob<IPaymentGatewayService>(
            It.IsAny<Expression<Action<IPaymentGatewayService>>>()), Times.Once);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldRollback_WhenPaymentConfirmationFails()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(false);

        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(confirmDto))
            .ReturnsAsync(Result<bool>.Failure("Payment not found."));

        // Act
        var result = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment not found.");

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        _mockOrderService.Verify(s => s.MarkOrderAsPaid(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldRollback_WhenOrderIdNotFound()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(false);

        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(confirmDto))
            .ReturnsAsync(Result<bool>.Success());

        _mockPaymentRepository.Setup(r => r.GetOrderIdByPaymentIdAsync(confirmDto.PaymentId))
            .ReturnsAsync((int?)null);

        // Act
        var result = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to retrieve order details.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldRollback_WhenMarkOrderAsPaidFails()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(false);

        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(confirmDto))
            .ReturnsAsync(Result<bool>.Success());

        _mockPaymentRepository.Setup(r => r.GetOrderIdByPaymentIdAsync(confirmDto.PaymentId))
            .ReturnsAsync(1);

        _mockOrderService.Setup(s => s.MarkOrderAsPaid(1))
            .ReturnsAsync(Result<bool>.Failure("Order already paid."));

        // Act
        var result = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Order already paid.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockInventoryReservationService.Verify(s => s.UpdateReservationStatusToConsumedAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldRollback_WhenInventoryReservationUpdateFails()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(false);

        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(confirmDto))
            .ReturnsAsync(Result<bool>.Success());

        _mockPaymentRepository.Setup(r => r.GetOrderIdByPaymentIdAsync(confirmDto.PaymentId))
            .ReturnsAsync(1);

        _mockOrderService.Setup(s => s.MarkOrderAsPaid(1))
            .ReturnsAsync(Result<bool>.Success());

        _mockInventoryReservationService.Setup(s => s.UpdateReservationStatusToConsumedAsync(1))
            .ReturnsAsync(Result<string>.Failure("Reservation not found."));

        // Act
        var result = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Reservation not found.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldRollbackAndReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(false);

        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(confirmDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("An unexpected error occurred during payment processing.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldHandleMultipleErrors_InErrorsList()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(false);

        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(confirmDto))
            .ReturnsAsync(Result<bool>.Failure(new List<string> { "Error 1", "Error 2", "Error 3" }));

        // Act
        var result = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
        result.Errors.Should().Contain("Error 3");
    }

    #endregion

    #endregion

    #region FailPaymentAndUpdateOrderAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task FailPaymentAndUpdateOrderAsync_ShouldReturnSuccess_WhenAllStepsSucceed()
    {
        // Arrange
        var failDto = CreateValidFailPaymentDto();

        _mockPaymentService.Setup(s => s.FailPaymentAsync(failDto.PaymentId, failDto.ProviderTransactionId))
            .ReturnsAsync(Result<int>.Success(1));

        _mockOrderService.Setup(s => s.UpdateOrderStatusToFailed(1))
            .ReturnsAsync(Result<string>.Success());

        // Act
        var result = await _sut.FailPaymentAndUpdateOrderAsync(failDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [Theory]
    [InlineData(1, "txn_123")]
    [InlineData(100, "txn_abc")]
    [InlineData(999, "txn_xyz")]
    public async Task FailPaymentAndUpdateOrderAsync_ShouldHandleDifferentPaymentIds_Successfully(int paymentId, string transactionId)
    {
        // Arrange
        var failDto = new FailPaymentDto
        {
            PaymentId = paymentId,
            ProviderTransactionId = transactionId
        };

        _mockPaymentService.Setup(s => s.FailPaymentAsync(paymentId, transactionId))
            .ReturnsAsync(Result<int>.Success(1));

        _mockOrderService.Setup(s => s.UpdateOrderStatusToFailed(1))
            .ReturnsAsync(Result<string>.Success());

        // Act
        var result = await _sut.FailPaymentAndUpdateOrderAsync(failDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task FailPaymentAndUpdateOrderAsync_ShouldRollback_WhenPaymentFailUpdateFails()
    {
        // Arrange
        var failDto = CreateValidFailPaymentDto();

        _mockPaymentService.Setup(s => s.FailPaymentAsync(failDto.PaymentId, failDto.ProviderTransactionId))
            .ReturnsAsync(Result<int>.Failure("Payment not found."));

        // Act
        var result = await _sut.FailPaymentAndUpdateOrderAsync(failDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment not found.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockOrderService.Verify(s => s.UpdateOrderStatusToFailed(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task FailPaymentAndUpdateOrderAsync_ShouldRollback_WhenOrderStatusUpdateFails()
    {
        // Arrange
        var failDto = CreateValidFailPaymentDto();

        _mockPaymentService.Setup(s => s.FailPaymentAsync(failDto.PaymentId, failDto.ProviderTransactionId))
            .ReturnsAsync(Result<int>.Success(1));

        _mockOrderService.Setup(s => s.UpdateOrderStatusToFailed(1))
            .ReturnsAsync(Result<string>.Failure("Cannot mark paid order as failed."));

        // Act
        var result = await _sut.FailPaymentAndUpdateOrderAsync(failDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot mark paid order as failed.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task FailPaymentAndUpdateOrderAsync_ShouldRollbackAndReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        var failDto = CreateValidFailPaymentDto();

        _mockPaymentService.Setup(s => s.FailPaymentAsync(failDto.PaymentId, failDto.ProviderTransactionId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.FailPaymentAndUpdateOrderAsync(failDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("An unexpected error occurred during payment failure processing.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    #endregion

    #endregion

    #region CancelPaymentAndUpdateOrderAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CancelPaymentAndUpdateOrderAsync_ShouldReturnSuccess_WhenAllStepsSucceed()
    {
        // Arrange
        var cancelDto = CreateValidCancelPaymentDto();

        _mockPaymentService.Setup(s => s.CancelPaymentAsync(cancelDto.PaymentId, cancelDto.ProviderTransactionId))
            .ReturnsAsync(Result<int>.Success(1));

        _mockOrderService.Setup(s => s.UpdateOrderStatusToCancelled(1))
            .ReturnsAsync(Result<string>.Success());

        // Act
        var result = await _sut.CancelPaymentAndUpdateOrderAsync(cancelDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelPaymentAndUpdateOrderAsync_ShouldCallStepsInCorrectOrder()
    {
        // Arrange
        var cancelDto = CreateValidCancelPaymentDto();
        var callOrder = new List<string>();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
            .Callback(() => callOrder.Add("BeginTransaction"))
            .Returns(Task.CompletedTask);

        _mockPaymentService.Setup(s => s.CancelPaymentAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Callback(() => callOrder.Add("CancelPayment"))
            .ReturnsAsync(Result<int>.Success(1));

        _mockOrderService.Setup(s => s.UpdateOrderStatusToCancelled(It.IsAny<int>()))
            .Callback(() => callOrder.Add("UpdateOrder"))
            .ReturnsAsync(Result<string>.Success());

        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Callback(() => callOrder.Add("Commit"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.CancelPaymentAndUpdateOrderAsync(cancelDto);

        // Assert
        callOrder.Should().Equal("BeginTransaction", "CancelPayment", "UpdateOrder", "Commit");
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task CancelPaymentAndUpdateOrderAsync_ShouldRollback_WhenPaymentCancellationFails()
    {
        // Arrange
        var cancelDto = CreateValidCancelPaymentDto();

        _mockPaymentService.Setup(s => s.CancelPaymentAsync(cancelDto.PaymentId, cancelDto.ProviderTransactionId))
            .ReturnsAsync(Result<int>.Failure("Payment already cancelled."));

        // Act
        var result = await _sut.CancelPaymentAndUpdateOrderAsync(cancelDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment already cancelled.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockOrderService.Verify(s => s.UpdateOrderStatusToCancelled(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CancelPaymentAndUpdateOrderAsync_ShouldRollback_WhenOrderCancellationFails()
    {
        // Arrange
        var cancelDto = CreateValidCancelPaymentDto();

        _mockPaymentService.Setup(s => s.CancelPaymentAsync(cancelDto.PaymentId, cancelDto.ProviderTransactionId))
            .ReturnsAsync(Result<int>.Success(1));

        _mockOrderService.Setup(s => s.UpdateOrderStatusToCancelled(1))
            .ReturnsAsync(Result<string>.Failure("Cannot cancel paid order."));

        // Act
        var result = await _sut.CancelPaymentAndUpdateOrderAsync(cancelDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot cancel paid order.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task CancelPaymentAndUpdateOrderAsync_ShouldRollbackAndReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        var cancelDto = CreateValidCancelPaymentDto();

        _mockPaymentService.Setup(s => s.CancelPaymentAsync(cancelDto.PaymentId, cancelDto.ProviderTransactionId))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _sut.CancelPaymentAndUpdateOrderAsync(cancelDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("An unexpected error occurred during payment cancellation processing.");

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    #endregion

    #endregion

    #region GetOrderNumberByPaymentId Tests

    [Theory]
    [InlineData(1, "ORD-123")]
    [InlineData(100, "ORD-456")]
    [InlineData(999, "ORD-789")]
    public async Task GetOrderNumberByPaymentId_ShouldReturnOrderNumber_WhenPaymentIdIsValid(int paymentId, string orderNumber)
    {
        // Arrange
        _mockOrderService.Setup(s => s.GetOrderNumberByPaymentId(paymentId))
            .ReturnsAsync(orderNumber);

        // Act
        var result = await _sut.GetOrderNumberByPaymentId(paymentId);

        // Assert
        result.Should().Be(orderNumber);
        _mockOrderService.Verify(s => s.GetOrderNumberByPaymentId(paymentId), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetOrderNumberByPaymentId_ShouldReturnEmptyString_WhenPaymentIdIsInvalid(int paymentId)
    {
        // Arrange & Act
        var result = await _sut.GetOrderNumberByPaymentId(paymentId);

        // Assert
        result.Should().Be(string.Empty);
        _mockOrderService.Verify(s => s.GetOrderNumberByPaymentId(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderNumberByPaymentId_ShouldReturnEmptyString_WhenOrderServiceReturnsEmpty()
    {
        // Arrange
        _mockOrderService.Setup(s => s.GetOrderNumberByPaymentId(1))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _sut.GetOrderNumberByPaymentId(1);

        // Assert
        result.Should().Be(string.Empty);
    }

    #endregion

    #region SendOrderConfirmationEmailAsync Tests

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_ShouldSendEmail_WhenOrderExists()
    {
        // Arrange
        int orderId = 1;
        var order = CreateOrder(orderId);

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _mockNotificationOrchestrator.Setup(n => n.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        // Act
        await _sut.SendOrderConfirmationEmailAsync(orderId);

        // Assert
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockNotificationOrchestrator.Verify(n => n.SendAsync(It.Is<EmailMessage>(
            e => e.To == order.UserId &&
                 e.Subject == "Order Confirmation - KeystoneCommerce" &&
                 e.Body == order.OrderNumber &&
                 e.NotificationType == NotificationType.OrderConfirmation)), Times.Once);
    }

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_ShouldNotSendEmail_WhenOrderNotFound()
    {
        // Arrange
        int orderId = 999;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        await _sut.SendOrderConfirmationEmailAsync(orderId);

        // Assert
        _mockNotificationOrchestrator.Verify(n => n.SendAsync(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_ShouldNotThrowException_WhenEmailSendingFails()
    {
        // Arrange
        int orderId = 1;
        var order = CreateOrder(orderId);

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _mockNotificationOrchestrator.Setup(n => n.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.SendOrderConfirmationEmailAsync(orderId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_ShouldHandleException_Gracefully()
    {
        // Arrange
        int orderId = 1;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var act = async () => await _sut.SendOrderConfirmationEmailAsync(orderId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_ShouldSetCorrectEmailProperties()
    {
        // Arrange
        int orderId = 1;
        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-ABC123",
            UserId = "user-xyz",
            SubTotal = 100m,
            Total = 110m,
            Shipping = 10m,
            Discount = 0m,
            IsPaid = true,
            Status = OrderStatus.Paid
        };

        EmailMessage? capturedEmail = null;

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _mockNotificationOrchestrator.Setup(n => n.SendAsync(It.IsAny<EmailMessage>()))
            .Callback<EmailMessage>(e => capturedEmail = e)
            .ReturnsAsync(true);

        // Act
        await _sut.SendOrderConfirmationEmailAsync(orderId);

        // Assert
        capturedEmail.Should().NotBeNull();
        capturedEmail!.To.Should().Be("user-xyz");
        capturedEmail.Subject.Should().Be("Order Confirmation - KeystoneCommerce");
        capturedEmail.Body.Should().Be("ORD-ABC123");
        capturedEmail.NotificationType.Should().Be(NotificationType.OrderConfirmation);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ConfirmPaymentAndUpdateOrderAsync_ShouldHandleConcurrentRequests_ByCheckingFulfillment()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        // Setup sequence for fulfillment check - first call returns false, second returns true
        _mockPaymentRepository.SetupSequence(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(false)  // First call - not fulfilled
            .ReturnsAsync(true);  // Second call - already fulfilled

        // Setup for first successful confirmation
        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(confirmDto))
            .ReturnsAsync(Result<bool>.Success());

        _mockPaymentRepository.Setup(r => r.GetOrderIdByPaymentIdAsync(confirmDto.PaymentId))
            .ReturnsAsync(1);

        _mockOrderService.Setup(s => s.MarkOrderAsPaid(1))
            .ReturnsAsync(Result<bool>.Success());

        _mockInventoryReservationService.Setup(s => s.UpdateReservationStatusToConsumedAsync(1))
            .ReturnsAsync(Result<string>.Success());

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result1 = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);
        var result2 = await _sut.ConfirmPaymentAndUpdateOrderAsync(confirmDto);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        // Fulfillment check should be called twice
        _mockPaymentRepository.Verify(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId), Times.Exactly(2));
        
        // Transaction should only begin once (second call short-circuits)
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        
        // Payment confirmation should only happen once
        _mockPaymentService.Verify(s => s.ConfirmPaymentAsync(It.IsAny<ConfirmPaymentDto>()), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentSessionAsync_ShouldHandleVeryLargeAmounts()
    {
        // Arrange
        var sessionDto = new CreatePaymentSessionDto
        {
            PaymentId = 1,
            TotalPrice = 999999999.99m,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        var expectedResult = new PaymentSessionResultDto
        {
            SessionId = "session_1",
            PaymentUrl = "https://checkout.stripe.com/session_1",
            PaymentId = 1
        };

        _mockStripPaymentService.Setup(s => s.CreatePaymentSessionAsync(It.IsAny<CreatePaymentSessionDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.CreatePaymentSessionAsync(sessionDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePaymentSessionAsync_ShouldHandleMinimalAmount()
    {
        // Arrange
        var sessionDto = new CreatePaymentSessionDto
        {
            PaymentId = 1,
            TotalPrice = 0.01m,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };

        var expectedResult = new PaymentSessionResultDto
        {
            SessionId = "session_1",
            PaymentUrl = "https://checkout.stripe.com/session_1",
            PaymentId = 1
        };

        _mockStripPaymentService.Setup(s => s.CreatePaymentSessionAsync(It.IsAny<CreatePaymentSessionDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.CreatePaymentSessionAsync(sessionDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static CreatePaymentSessionDto CreateValidSessionDto()
    {
        return new CreatePaymentSessionDto
        {
            PaymentId = 1,
            TotalPrice = 100.50m,
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        };
    }

    private static ConfirmPaymentDto CreateValidConfirmPaymentDto()
    {
        return new ConfirmPaymentDto
        {
            PaymentId = 1,
            ProviderTransactionId = "txn_123456",
            Amount = 100.50m
        };
    }

    private static FailPaymentDto CreateValidFailPaymentDto()
    {
        return new FailPaymentDto
        {
            PaymentId = 1,
            ProviderTransactionId = "txn_failed_123"
        };
    }

    private static CancelPaymentDto CreateValidCancelPaymentDto()
    {
        return new CancelPaymentDto
        {
            PaymentId = 1,
            ProviderTransactionId = "txn_cancelled_123"
        };
    }

    private static Order CreateOrder(int id)
    {
        return new Order
        {
            Id = id,
            OrderNumber = $"ORD-{id}",
            UserId = "user-123",
            SubTotal = 100m,
            Total = 110m,
            Shipping = 10m,
            Discount = 0m,
            IsPaid = true,
            Status = OrderStatus.Paid
        };
    }

    private void SetupSuccessfulPaymentConfirmation(ConfirmPaymentDto confirmDto)
    {
        _mockPaymentRepository.Setup(r => r.IsPaymentFulfilledAsync(confirmDto.PaymentId))
            .ReturnsAsync(false);

        _mockPaymentService.Setup(s => s.ConfirmPaymentAsync(confirmDto))
            .ReturnsAsync(Result<bool>.Success());

        _mockPaymentRepository.Setup(r => r.GetOrderIdByPaymentIdAsync(confirmDto.PaymentId))
            .ReturnsAsync(1);

        _mockOrderService.Setup(s => s.MarkOrderAsPaid(1))
            .ReturnsAsync(Result<bool>.Success());

        _mockInventoryReservationService.Setup(s => s.UpdateReservationStatusToConsumedAsync(1))
            .ReturnsAsync(Result<string>.Success());

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
    }

    private void VerifyAllConfirmationStepsCalled()
    {
        _mockPaymentRepository.Verify(r => r.IsPaymentFulfilledAsync(It.IsAny<int>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockPaymentService.Verify(s => s.ConfirmPaymentAsync(It.IsAny<ConfirmPaymentDto>()), Times.Once);
        _mockPaymentRepository.Verify(r => r.GetOrderIdByPaymentIdAsync(It.IsAny<int>()), Times.Once);
        _mockOrderService.Verify(s => s.MarkOrderAsPaid(It.IsAny<int>()), Times.Once);
        _mockInventoryReservationService.Verify(s => s.UpdateReservationStatusToConsumedAsync(It.IsAny<int>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _mockBackgroundService.Verify(s => s.EnqueueJob<IPaymentGatewayService>(It.IsAny<Expression<Action<IPaymentGatewayService>>>()), Times.Once);
    }

    #endregion
}
