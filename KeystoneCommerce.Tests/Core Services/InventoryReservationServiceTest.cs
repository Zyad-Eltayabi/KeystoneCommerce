namespace KeystoneCommerce.Tests.Core_Services;

[Collection("InventoryReservationServiceTests")]
public class InventoryReservationServiceTest
{
    private readonly Mock<IInventoryReservationRepository> _mockInventoryReservationRepository;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ILogger<InventoryReservationService>> _mockLogger;
    private readonly Mock<IOptions<InventorySettings>> _mockInventorySettings;
    private readonly Mock<IBackgroundService> _mockBackgroundService;
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly InventoryReservationService _sut;
    private readonly InventorySettings _inventorySettings;

    public InventoryReservationServiceTest()
    {
        _mockInventoryReservationRepository = new Mock<IInventoryReservationRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<InventoryReservationService>>();
        _mockBackgroundService = new Mock<IBackgroundService>();
        _mockOrderService = new Mock<IOrderService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _inventorySettings = new InventorySettings
        {
            ReservationExpirationMinutes = 30
        };
        _mockInventorySettings = new Mock<IOptions<InventorySettings>>();
        _mockInventorySettings.Setup(x => x.Value).Returns(_inventorySettings);

        _sut = new InventoryReservationService(
            _mockInventoryReservationRepository.Object,
            _mockOrderRepository.Object,
            _mockLogger.Object,
            _mockInventorySettings.Object,
            _mockBackgroundService.Object,
            _mockOrderService.Object,
            _mockUnitOfWork.Object);
    }

    #region CreateReservationAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CreateReservationAsync_ShouldReturnSuccess_WhenOrderExistsAndStripePayment()
    {
        // Arrange
        int orderId = 1;
        var paymentType = PaymentType.Stripe;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        TimeSpan expectedDelay = TimeSpan.Zero;
        _mockInventoryReservationRepository.Setup(r => r.AddAsync(It.IsAny<InventoryReservation>()))
            .Callback<InventoryReservation>(ir => expectedDelay = (ir.ExpiresAt - DateTime.UtcNow)!.Value)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateReservationAsync(orderId, paymentType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("Reservation created successfully");
        result.Errors.Should().BeEmpty();

        _mockOrderRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.AddAsync(It.IsAny<InventoryReservation>()), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockBackgroundService.Verify(
            b => b.ScheduleJob(It.IsAny<Expression<Action<IInventoryReservationService>>>(), It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldReturnSuccess_WhenOrderExistsAndCashOnDelivery()
    {
        // Arrange
        int orderId = 2;
        var paymentType = PaymentType.CashOnDelivery;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.CreateReservationAsync(orderId, paymentType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("Reservation created successfully");
        result.Errors.Should().BeEmpty();

        _mockOrderRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.AddAsync(It.IsAny<InventoryReservation>()), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        // Should NOT schedule background job for COD
        _mockBackgroundService.Verify(
            b => b.ScheduleJob(It.IsAny<Expression<Action<IInventoryReservationService>>>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public async Task CreateReservationAsync_ShouldHandleDifferentOrderIds_Successfully(int orderId)
    {
        // Arrange
        var paymentType = PaymentType.Stripe;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.CreateReservationAsync(orderId, paymentType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockInventoryReservationRepository.Verify(
            r => r.AddAsync(It.Is<InventoryReservation>(ir => ir.OrderId == orderId)),
            Times.Once);

        _mockBackgroundService.Verify(
           b => b.ScheduleJob(It.IsAny<Expression<Action<IInventoryReservationService>>>(), It.IsAny<TimeSpan>()),
           Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldSetExpirationTime_WhenStripePayment()
    {
        // Arrange
        int orderId = 1;
        var paymentType = PaymentType.Stripe;
        InventoryReservation? capturedReservation = null;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.AddAsync(It.IsAny<InventoryReservation>()))
            .Callback<InventoryReservation>(ir => capturedReservation = ir);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        var beforeExecution = DateTime.UtcNow.AddMinutes(_inventorySettings.ReservationExpirationMinutes);

        // Act
        await _sut.CreateReservationAsync(orderId, paymentType);

        var afterExecution = DateTime.UtcNow.AddMinutes(_inventorySettings.ReservationExpirationMinutes);

        // Assert
        capturedReservation.Should().NotBeNull();
        capturedReservation!.ExpiresAt.Should().NotBeNull();
        capturedReservation.ExpiresAt!.Value.Should().BeOnOrAfter(beforeExecution.AddSeconds(-1));
        capturedReservation.ExpiresAt.Value.Should().BeOnOrBefore(afterExecution.AddSeconds(1));
        capturedReservation.Status.Should().Be(ReservationStatus.Active);
        capturedReservation.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldNotSetExpirationTime_WhenCashOnDelivery()
    {
        // Arrange
        int orderId = 1;
        var paymentType = PaymentType.CashOnDelivery;
        InventoryReservation? capturedReservation = null;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.AddAsync(It.IsAny<InventoryReservation>()))
            .Callback<InventoryReservation>(ir => capturedReservation = ir);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.CreateReservationAsync(orderId, paymentType);

        // Assert
        capturedReservation.Should().NotBeNull();
        capturedReservation!.ExpiresAt.Should().BeNull();
        capturedReservation.Status.Should().Be(ReservationStatus.Active);
        capturedReservation.OrderId.Should().Be(orderId);
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task CreateReservationAsync_ShouldReturnFailure_WhenOrderDoesNotExist()
    {
        // Arrange
        int orderId = 999;
        var paymentType = PaymentType.Stripe;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateReservationAsync(orderId, paymentType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Order does not exist");
        result.Data.Should().BeNull();

        _mockOrderRepository.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.AddAsync(It.IsAny<InventoryReservation>()), Times.Never);
        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        _mockBackgroundService.Verify(
            b => b.ScheduleJob(It.IsAny<Expression<Action<IInventoryReservationService>>>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task CreateReservationAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        int orderId = 1;
        var paymentType = PaymentType.Stripe;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(0);

        // Act
        var result = await _sut.CreateReservationAsync(orderId, paymentType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Failed to create reservation");
        result.Data.Should().BeNull();

        _mockInventoryReservationRepository.Verify(r => r.AddAsync(It.IsAny<InventoryReservation>()), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _mockBackgroundService.Verify(
            b => b.ScheduleJob(It.IsAny<Expression<Action<IInventoryReservationService>>>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldScheduleBackgroundJob_OnlyForNonCODPayments()
    {
        // Arrange
        int orderId = 1;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act - Test with Stripe
        await _sut.CreateReservationAsync(orderId, PaymentType.Stripe);

        // Assert
        _mockBackgroundService.Verify(
            b => b.ScheduleJob(It.IsAny<Expression<Action<IInventoryReservationService>>>(), It.IsAny<TimeSpan>()),
            Times.Once);

        // Reset
        _mockBackgroundService.Invocations.Clear();

        // Act - Test with COD
        await _sut.CreateReservationAsync(orderId, PaymentType.CashOnDelivery);

        // Assert
        _mockBackgroundService.Verify(
            b => b.ScheduleJob(It.IsAny<Expression<Action<IInventoryReservationService>>>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(60)]
    [InlineData(1440)] // 24 hours
    public async Task CreateReservationAsync_ShouldHandleDifferentExpirationMinutes_Successfully(int expirationMinutes)
    {
        // Arrange
        int orderId = 1;
        var paymentType = PaymentType.Stripe;
        _inventorySettings.ReservationExpirationMinutes = expirationMinutes;
        InventoryReservation? capturedReservation = null;

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.AddAsync(It.IsAny<InventoryReservation>()))
            .Callback<InventoryReservation>(ir => capturedReservation = ir);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        var expectedExpiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // Act
        await _sut.CreateReservationAsync(orderId, paymentType);

        // Assert
        capturedReservation.Should().NotBeNull();
        capturedReservation!.ExpiresAt.Should().NotBeNull();
        capturedReservation.ExpiresAt!.Value.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(2));
    }

    #endregion

    #endregion

    #region UpdateReservationStatusToConsumedAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task UpdateReservationStatusToConsumedAsync_ShouldReturnSuccess_WhenReservationIsActive()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateReservationStatusToConsumedAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("Reservation status updated to consumed");
        result.Errors.Should().BeEmpty();
        reservation.Status.Should().Be(ReservationStatus.Consumed);

        _mockInventoryReservationRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.Update(reservation), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public async Task UpdateReservationStatusToConsumedAsync_ShouldHandleDifferentOrderIds_Successfully(int orderId)
    {
        // Arrange
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateReservationStatusToConsumedAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task UpdateReservationStatusToConsumedAsync_ShouldReturnFailure_WhenReservationNotFound()
    {
        // Arrange
        int orderId = 999;

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync((InventoryReservation?)null);

        // Act
        var result = await _sut.UpdateReservationStatusToConsumedAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("No reservation found for this order");
        result.Data.Should().BeNull();

        _mockInventoryReservationRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.Update(It.IsAny<InventoryReservation>()), Times.Never);
        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData(ReservationStatus.Released)]
    [InlineData(ReservationStatus.Consumed)]
    public async Task UpdateReservationStatusToConsumedAsync_ShouldReturnFailure_WhenReservationNotActive(ReservationStatus status)
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = status
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _sut.UpdateReservationStatusToConsumedAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain($"Reservation is not active. Current status: {status}");
        result.Data.Should().BeNull();

        _mockInventoryReservationRepository.Verify(r => r.Update(It.IsAny<InventoryReservation>()), Times.Never);
        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task UpdateReservationStatusToConsumedAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(0);

        // Act
        var result = await _sut.UpdateReservationStatusToConsumedAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.Should().Contain("Failed to update reservation status");
        result.Data.Should().BeNull();

        _mockInventoryReservationRepository.Verify(r => r.Update(reservation), Times.Once);
        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationStatusToConsumedAsync_ShouldUpdateStatusToConsumed_WhenSuccessful()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateReservationStatusToConsumedAsync(orderId);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Consumed);
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #endregion

    #region CheckExpiredReservation Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CheckExpiredReservation_ShouldReleaseReservation_WhenReservationIsActive()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // Expired
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockOrderService.Setup(s => s.ReleaseReservedStock(orderId))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.CheckExpiredReservation(orderId);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Released);

        _mockInventoryReservationRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);

        _mockOrderService.Verify(s => s.ReleaseReservedStock(orderId), Times.Once);

        _mockInventoryReservationRepository.Verify(r => r.Update(reservation), Times.Once);

        _mockInventoryReservationRepository.Verify(r => r.SaveChangesAsync(), Times.Once);

        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);

        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task CheckExpiredReservation_ShouldNotProcess_WhenReservationNotFound()
    {
        // Arrange
        int orderId = 999;

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync((InventoryReservation?)null);

        // Act
        await _sut.CheckExpiredReservation(orderId);

        // Assert
        _mockInventoryReservationRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _mockOrderService.Verify(s => s.ReleaseReservedStock(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CheckExpiredReservation_ShouldNotProcess_WhenReservationAlreadyReleased()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Released
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);

        // Act
        await _sut.CheckExpiredReservation(orderId);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _mockOrderService.Verify(s => s.ReleaseReservedStock(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CheckExpiredReservation_ShouldNotProcess_WhenReservationAlreadyConsumed()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Consumed
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);

        // Act
        await _sut.CheckExpiredReservation(orderId);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _mockOrderService.Verify(s => s.ReleaseReservedStock(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Error Handling Scenarios

    [Fact]
    public async Task CheckExpiredReservation_ShouldRollback_WhenReleaseStockFails()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockOrderService.Setup(s => s.ReleaseReservedStock(orderId))
            .ReturnsAsync(false);

        // Act
        await _sut.CheckExpiredReservation(orderId);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Active); // Should not change

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockOrderService.Verify(s => s.ReleaseReservedStock(orderId), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        _mockInventoryReservationRepository.Verify(r => r.Update(It.IsAny<InventoryReservation>()), Times.Never);
    }

    [Fact]
    public async Task CheckExpiredReservation_ShouldRollback_WhenExceptionThrown()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockOrderService.Setup(s => s.ReleaseReservedStock(orderId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        await _sut.CheckExpiredReservation(orderId);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CheckExpiredReservation_ShouldHandleException_Gracefully()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockOrderService.Setup(s => s.ReleaseReservedStock(orderId))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var act = async () => await _sut.CheckExpiredReservation(orderId);

        // Assert
        await act.Should().NotThrowAsync();
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    #endregion

    #region Transaction Management

    [Fact]
    public async Task CheckExpiredReservation_ShouldCommitTransaction_WhenSuccessful()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active
        };

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation);
        _mockOrderService.Setup(s => s.ReleaseReservedStock(orderId))
            .ReturnsAsync(true);
        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _sut.CheckExpiredReservation(orderId);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task CheckExpiredReservation_ShouldFollowCorrectOrderOfOperations()
    {
        // Arrange
        int orderId = 1;
        var reservation = new InventoryReservation
        {
            Id = 1,
            OrderId = orderId,
            Status = ReservationStatus.Active
        };
        var callOrder = new List<string>();

        _mockInventoryReservationRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryReservation, bool>>>()))
            .ReturnsAsync(reservation)
            .Callback(() => callOrder.Add("Find"));

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync())
            .Callback(() => callOrder.Add("BeginTransaction"))
            .Returns(Task.CompletedTask);

        _mockOrderService.Setup(s => s.ReleaseReservedStock(orderId))
            .Callback(() => callOrder.Add("ReleaseStock"))
            .ReturnsAsync(true);

        _mockInventoryReservationRepository.Setup(r => r.Update(It.IsAny<InventoryReservation>()))
            .Callback(() => callOrder.Add("Update"));

        _mockInventoryReservationRepository.Setup(r => r.SaveChangesAsync())
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Callback(() => callOrder.Add("Commit"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.CheckExpiredReservation(orderId);

        // Assert
        callOrder.Should().Equal("Find", "BeginTransaction", "ReleaseStock", "Update", "SaveChanges", "Commit");
    }

    #endregion

    #endregion
}
