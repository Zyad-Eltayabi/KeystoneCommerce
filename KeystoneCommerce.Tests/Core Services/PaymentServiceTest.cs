using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Infrastructure.Validation.Validators.Payment;
using PaymentDetailsDto = KeystoneCommerce.Application.DTOs.Payment.PaymentDetailsDto;

namespace KeystoneCommerce.Tests.Core_Services;

[Collection("PaymentServiceTests")]
public class PaymentServiceTest
{
    private readonly Mock<IPaymentRepository> _mockPaymentRepository;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ILogger<PaymentService>> _mockLogger;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly IApplicationValidator<CreatePaymentDto> _validator;
    private readonly IMappingService _mappingService;
    private readonly PaymentService _sut;

    public PaymentServiceTest()
    {
        _mockPaymentRepository = new Mock<IPaymentRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<PaymentService>>();
        _mockIdentityService = new Mock<IIdentityService>();
        _mockCacheService = new Mock<ICacheService>();

        var fluentValidator = new CreatePaymentDtoValidator();
        _validator = new FluentValidationAdapter<CreatePaymentDto>(fluentValidator);
        _mappingService = new MappingService(MapperHelper.CreateMapper());

        _sut = new PaymentService(
            _validator,
            _mockPaymentRepository.Object,
            _mockOrderRepository.Object,
            _mappingService,
            _mockLogger.Object,
            _mockIdentityService.Object,
            _mockCacheService.Object);
    }

    #region CreatePaymentAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnSuccess_WhenValidPaymentDto()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();

        _mockPaymentRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .Returns(Task.CompletedTask);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        _mockPaymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
        _mockPaymentRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Dashboard:"), Times.Once);
    }

    [Theory]
    [InlineData(PaymentType.Stripe)]
    [InlineData(PaymentType.CashOnDelivery)]
    public async Task CreatePaymentAsync_ShouldSucceed_WithDifferentPaymentProviders(PaymentType provider)
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.Provider = provider;

        _mockPaymentRepository.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(100.00)]
    [InlineData(999999.99)]
    public async Task CreatePaymentAsync_ShouldSucceed_WithDifferentAmounts(decimal amount)
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.Amount = amount;

        _mockPaymentRepository.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenAmountIsZero()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.Amount = 0;

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment amount must be greater than 0.");

        _mockPaymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenAmountIsNegative()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.Amount = -10;

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment amount must be greater than 0.");
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenCurrencyIsEmpty()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.Currency = string.Empty;

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Currency is required.");
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenUserIdIsEmpty()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.UserId = string.Empty;

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User ID is required.");
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenOrderIdIsZero()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.OrderId = 0;

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Order ID must be greater than 0.");
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenCurrencyExceedsMaxLength()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.Currency = new string('A', 11); // Max is 10

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Currency must not exceed 10 characters.");
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenProviderTransactionIdExceedsMaxLength()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.ProviderTransactionId = new string('A', 201); // Max is 200

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Provider transaction ID must not exceed 200 characters.");
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenMultipleValidationErrors()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();
        createPaymentDto.Amount = 0;
        createPaymentDto.Currency = string.Empty;
        createPaymentDto.UserId = string.Empty;

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Repository Failure Scenarios

    [Fact]
    public async Task CreatePaymentAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();

        _mockPaymentRepository.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to create payment.");
    }

    #endregion

    #endregion

    #region ConfirmPaymentAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task ConfirmPaymentAsync_ShouldReturnSuccess_WhenPaymentExistsAndAmountMatches()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();
        var payment = CreatePayment(confirmDto.PaymentId, confirmDto.Amount);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(confirmDto.PaymentId))
            .ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.ConfirmPaymentAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Successful);
        payment.IsFulfilled.Should().BeTrue();
        payment.ProviderTransactionId.Should().Be(confirmDto.ProviderTransactionId);

        _mockPaymentRepository.Verify(r => r.Update(payment), Times.Once);
        _mockPaymentRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        
        _mockCacheService.Verify(c => c.Remove($"Payment:Details:{confirmDto.PaymentId}"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Dashboard:"), Times.Once);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task ConfirmPaymentAsync_ShouldReturnFailure_WhenPaymentNotFound()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(confirmDto.PaymentId))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _sut.ConfirmPaymentAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment not found.");

        _mockPaymentRepository.Verify(r => r.Update(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ShouldReturnFailure_WhenAmountDoesNotMatch()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();
        var payment = CreatePayment(confirmDto.PaymentId, 50.00m); // Different amount

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(confirmDto.PaymentId))
            .ReturnsAsync(payment);

        // Act
        var result = await _sut.ConfirmPaymentAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment amount does not match the order amount.");

        _mockPaymentRepository.Verify(r => r.Update(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();
        var payment = CreatePayment(confirmDto.PaymentId, confirmDto.Amount);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(confirmDto.PaymentId))
            .ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.ConfirmPaymentAsync(confirmDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to confirm payment.");
    }

    #endregion

    #endregion

    #region FailPaymentAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task FailPaymentAsync_ShouldReturnSuccess_WhenPaymentCanBeFailed()
    {
        // Arrange
        int paymentId = 1;
        string transactionId = "txn_failed_123";
        var payment = CreatePayment(paymentId, 100m, PaymentStatus.Processing, false);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.FailPaymentAsync(paymentId, transactionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(payment.OrderId);
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.ProviderTransactionId.Should().Be(transactionId);

        _mockPaymentRepository.Verify(r => r.Update(payment), Times.Once);
        
        _mockCacheService.Verify(c => c.Remove($"Payment:Details:{paymentId}"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Dashboard:"), Times.Once);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task FailPaymentAsync_ShouldReturnFailure_WhenPaymentNotFound()
    {
        // Arrange
        _mockPaymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _sut.FailPaymentAsync(999, "txn_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment not found.");
    }

    [Fact]
    public async Task FailPaymentAsync_ShouldReturnFailure_WhenPaymentIsAlreadyFulfilled()
    {
        // Arrange
        var payment = CreatePayment(1, 100m, PaymentStatus.Successful, true);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);

        // Act
        var result = await _sut.FailPaymentAsync(1, "txn_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot mark a fulfilled payment as failed.");
    }

    [Fact]
    public async Task FailPaymentAsync_ShouldReturnFailure_WhenPaymentIsAlreadySuccessful()
    {
        // Arrange
        var payment = CreatePayment(1, 100m, PaymentStatus.Successful, false);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);

        // Act
        var result = await _sut.FailPaymentAsync(1, "txn_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot mark a successful payment as failed.");
    }

    [Fact]
    public async Task FailPaymentAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var payment = CreatePayment(1, 100m, PaymentStatus.Processing, false);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.FailPaymentAsync(1, "txn_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to update payment status.");
    }

    #endregion

    #endregion

    #region CancelPaymentAsync Tests

    #region Happy Path Scenarios

    [Fact]
    public async Task CancelPaymentAsync_ShouldReturnSuccess_WhenPaymentCanBeCancelled()
    {
        // Arrange
        int paymentId = 1;
        string transactionId = "txn_cancelled_123";
        var payment = CreatePayment(paymentId, 100m, PaymentStatus.Processing, false);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _sut.CancelPaymentAsync(paymentId, transactionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(payment.OrderId);
        payment.Status.Should().Be(PaymentStatus.Canceled);
        payment.ProviderTransactionId.Should().Be(transactionId);
        
        _mockCacheService.Verify(c => c.Remove($"Payment:Details:{paymentId}"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Dashboard:"), Times.Once);
    }

    #endregion

    #region Business Logic Scenarios

    [Fact]
    public async Task CancelPaymentAsync_ShouldReturnFailure_WhenPaymentNotFound()
    {
        // Arrange
        _mockPaymentRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _sut.CancelPaymentAsync(999, "txn_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment not found.");
    }

    [Fact]
    public async Task CancelPaymentAsync_ShouldReturnFailure_WhenPaymentIsAlreadyFulfilled()
    {
        // Arrange
        var payment = CreatePayment(1, 100m, PaymentStatus.Successful, true);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);

        // Act
        var result = await _sut.CancelPaymentAsync(1, "txn_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Cannot cancel a fulfilled payment.");
    }

    [Fact]
    public async Task CancelPaymentAsync_ShouldReturnFailure_WhenPaymentIsAlreadyCancelled()
    {
        // Arrange
        var payment = CreatePayment(1, 100m, PaymentStatus.Canceled, false);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);

        // Act
        var result = await _sut.CancelPaymentAsync(1, "txn_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment is already cancelled.");
    }

    [Fact]
    public async Task CancelPaymentAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var payment = CreatePayment(1, 100m, PaymentStatus.Processing, false);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _sut.CancelPaymentAsync(1, "txn_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Failed to update payment status.");
    }

    #endregion

    #endregion

    #region GetAllPaymentsPaginatedAsync Tests

    [Fact]
    public async Task GetAllPaymentsPaginatedAsync_ShouldReturnPaginatedResult_WithDefaultSorting()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };
        var payments = new List<Payment> { CreatePayment(1, 100m), CreatePayment(2, 200m) };

        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync(payments);

        // Act
        var result = await _sut.GetAllPaymentsPaginatedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllPaymentsPaginatedAsync_ShouldSetDefaultSorting_WhenSortByIsNull()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10, SortBy = null };
        var payments = new List<Payment>();

        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync(payments);

        // Act
        await _sut.GetAllPaymentsPaginatedAsync(parameters);

        // Assert
        parameters.SortBy.Should().Be("CreatedAt");
    }

    [Fact]
    public async Task GetAllPaymentsPaginatedAsync_ShouldReturnEmptyList_WhenNoPayments()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };

        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetAllPaymentsPaginatedAsync(parameters);

        // Assert
        result.Items.Should().BeEmpty();
    }

    #endregion

    #region GetPaymentDetailsByIdAsync Tests

    [Fact]
    public async Task GetPaymentDetailsByIdAsync_ShouldReturnSuccess_WhenPaymentAndUserExist()
    {
        // Arrange
        int paymentId = 1;
        var payment = CreatePayment(paymentId, 100m);
        var userInfo = new UserBasicInfoDto { Id = "user-123", FullName = "John Doe", Email = "john@example.com" };

        _mockPaymentRepository.Setup(r => r.GetPaymentDetailsByIdAsync(paymentId))
            .ReturnsAsync(payment);
        _mockIdentityService.Setup(s => s.GetUserBasicInfoByIdAsync(payment.UserId))
            .ReturnsAsync(userInfo);

        // Act
        var result = await _sut.GetPaymentDetailsByIdAsync(paymentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.User.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPaymentDetailsByIdAsync_ShouldReturnFailure_WhenPaymentNotFound()
    {
        // Arrange
        _mockPaymentRepository.Setup(r => r.GetPaymentDetailsByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _sut.GetPaymentDetailsByIdAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Payment not found.");
    }

    [Fact]
    public async Task GetPaymentDetailsByIdAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var payment = CreatePayment(1, 100m);

        _mockPaymentRepository.Setup(r => r.GetPaymentDetailsByIdAsync(1))
            .ReturnsAsync(payment);
        _mockIdentityService.Setup(s => s.GetUserBasicInfoByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((UserBasicInfoDto?)null);

        // Act
        var result = await _sut.GetPaymentDetailsByIdAsync(1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User information not found.");
    }

    #endregion

    #region GetPaymentDashboardDataAsync Tests

    [Fact]
    public async Task GetPaymentDashboardDataAsync_ShouldReturnDashboardData()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };
        var analytics = new PaymentAnalyticsDto { TotalPayments = 10, TotalRevenue = 1000m };

        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync([]);
        _mockPaymentRepository.Setup(r => r.GetTodayAnalyticsAsync()).ReturnsAsync(analytics);
        _mockPaymentRepository.Setup(r => r.GetLast7DaysAnalyticsAsync()).ReturnsAsync(analytics);
        _mockPaymentRepository.Setup(r => r.GetLast30DaysAnalyticsAsync()).ReturnsAsync(analytics);

        // Act
        var result = await _sut.GetPaymentDashboardDataAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.PaginatedPayments.Should().NotBeNull();
        result.TodayAnalytics.Should().NotBeNull();
        result.Last7DaysAnalytics.Should().NotBeNull();
        result.Last30DaysAnalytics.Should().NotBeNull();
    }

    #endregion

    #region Caching Tests

    #region GetAllPaymentsPaginatedAsync Cache Tests

    [Fact]
    public async Task GetAllPaymentsPaginatedAsync_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };
        var cachedResult = new PaymentPaginatedResult<PaymentDto>
        {
            Items = [new PaymentDto { Id = 1, Amount = 100m }],
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _mockCacheService.Setup(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.IsAny<string>()))
            .Returns(cachedResult);

        // Act
        var result = await _sut.GetAllPaymentsPaginatedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Id.Should().Be(1);

        _mockCacheService.Verify(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.IsAny<string>()), Times.Once);
        _mockPaymentRepository.Verify(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()), Times.Never);
        _mockCacheService.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<PaymentPaginatedResult<PaymentDto>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetAllPaymentsPaginatedAsync_ShouldFetchFromDatabase_WhenCacheMiss()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };
        var payments = new List<Payment> { CreatePayment(1, 100m), CreatePayment(2, 200m) };

        _mockCacheService.Setup(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.IsAny<string>()))
            .Returns((PaymentPaginatedResult<PaymentDto>?)null);
        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync(payments);

        // Act
        var result = await _sut.GetAllPaymentsPaginatedAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);

        _mockCacheService.Verify(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.IsAny<string>()), Times.Once);
        _mockPaymentRepository.Verify(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()), Times.Once);
        _mockCacheService.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<PaymentPaginatedResult<PaymentDto>>(), TimeSpan.FromMinutes(5)), Times.Once);
    }

    [Fact]
    public async Task GetAllPaymentsPaginatedAsync_ShouldCacheResult_WithCorrectTTL()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };
        var payments = new List<Payment> { CreatePayment(1, 100m) };

        _mockCacheService.Setup(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.IsAny<string>()))
            .Returns((PaymentPaginatedResult<PaymentDto>?)null);
        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync(payments);

        // Act
        await _sut.GetAllPaymentsPaginatedAsync(parameters);

        // Assert
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<PaymentPaginatedResult<PaymentDto>>(), 
            TimeSpan.FromMinutes(5)), Times.Once);
    }

    [Fact]
    public async Task GetAllPaymentsPaginatedAsync_ShouldUseUniqueCacheKeys_ForDifferentParameters()
    {
        // Arrange
        var parameters1 = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10, Status = (int)PaymentStatus.Successful };
        var parameters2 = new PaymentPaginationParameters { PageNumber = 2, PageSize = 20, Status = (int)PaymentStatus.Failed };

        _mockCacheService.Setup(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.IsAny<string>()))
            .Returns((PaymentPaginatedResult<PaymentDto>?)null);
        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync([]);

        // Act
        await _sut.GetAllPaymentsPaginatedAsync(parameters1);
        await _sut.GetAllPaymentsPaginatedAsync(parameters2);

        // Assert
        _mockCacheService.Verify(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.Is<string>(key => key.Contains("Page1"))), Times.Once);
        _mockCacheService.Verify(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.Is<string>(key => key.Contains("Page2"))), Times.Once);
    }

    #endregion

    #region GetPaymentDetailsByIdAsync Cache Tests

    [Fact]
    public async Task GetPaymentDetailsByIdAsync_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        int paymentId = 1;
        var cachedPaymentDetails = new PaymentDetailsDto
        {
            Id = paymentId,
            Amount = 100m,
            User = new UserBasicInfoDto { Id = "user-123", FullName = "John Doe" }
        };

        _mockCacheService.Setup(c => c.Get<PaymentDetailsDto>(It.IsAny<string>()))
            .Returns(cachedPaymentDetails);

        // Act
        var result = await _sut.GetPaymentDetailsByIdAsync(paymentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(paymentId);
        result.Data.User.Should().NotBeNull();

        _mockCacheService.Verify(c => c.Get<PaymentDetailsDto>($"Payment:Details:{paymentId}"), Times.Once);
        _mockPaymentRepository.Verify(r => r.GetPaymentDetailsByIdAsync(It.IsAny<int>()), Times.Never);
        _mockIdentityService.Verify(s => s.GetUserBasicInfoByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPaymentDetailsByIdAsync_ShouldFetchFromDatabase_WhenCacheMiss()
    {
        // Arrange
        int paymentId = 1;
        var payment = CreatePayment(paymentId, 100m);
        var userInfo = new UserBasicInfoDto { Id = "user-123", FullName = "John Doe", Email = "john@example.com" };

        _mockCacheService.Setup(c => c.Get<PaymentDetailsDto>(It.IsAny<string>()))
            .Returns((PaymentDetailsDto?)null);
        _mockPaymentRepository.Setup(r => r.GetPaymentDetailsByIdAsync(paymentId))
            .ReturnsAsync(payment);
        _mockIdentityService.Setup(s => s.GetUserBasicInfoByIdAsync(payment.UserId))
            .ReturnsAsync(userInfo);

        // Act
        var result = await _sut.GetPaymentDetailsByIdAsync(paymentId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _mockCacheService.Verify(c => c.Get<PaymentDetailsDto>($"Payment:Details:{paymentId}"), Times.Once);
        _mockPaymentRepository.Verify(r => r.GetPaymentDetailsByIdAsync(paymentId), Times.Once);
        _mockIdentityService.Verify(s => s.GetUserBasicInfoByIdAsync(payment.UserId), Times.Once);
        _mockCacheService.Verify(c => c.Set(
            $"Payment:Details:{paymentId}", 
            It.IsAny<PaymentDetailsDto>(), 
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(5)), Times.Once);
    }

    [Fact]
    public async Task GetPaymentDetailsByIdAsync_ShouldCacheResult_WithSlidingExpiration()
    {
        // Arrange
        int paymentId = 1;
        var payment = CreatePayment(paymentId, 100m);
        var userInfo = new UserBasicInfoDto { Id = "user-123", FullName = "John Doe", Email = "john@example.com" };

        _mockCacheService.Setup(c => c.Get<PaymentDetailsDto>(It.IsAny<string>()))
            .Returns((PaymentDetailsDto?)null);
        _mockPaymentRepository.Setup(r => r.GetPaymentDetailsByIdAsync(paymentId))
            .ReturnsAsync(payment);
        _mockIdentityService.Setup(s => s.GetUserBasicInfoByIdAsync(payment.UserId))
            .ReturnsAsync(userInfo);

        // Act
        await _sut.GetPaymentDetailsByIdAsync(paymentId);

        // Assert
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<PaymentDetailsDto>(), 
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(5)), Times.Once);
    }

    #endregion

    #region GetPaymentDashboardDataAsync Cache Tests

    [Fact]
    public async Task GetPaymentDashboardDataAsync_ShouldReturnFromCache_WhenCacheHit()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };
        var cachedDashboard = new PaymentDashboardDto
        {
            PaginatedPayments = new PaymentPaginatedResult<PaymentDto> { Items = [] },
            TodayAnalytics = new PaymentAnalyticsDto { TotalPayments = 5 },
            Last7DaysAnalytics = new PaymentAnalyticsDto { TotalPayments = 50 },
            Last30DaysAnalytics = new PaymentAnalyticsDto { TotalPayments = 200 }
        };

        _mockCacheService.Setup(c => c.Get<PaymentDashboardDto>(It.IsAny<string>()))
            .Returns(cachedDashboard);

        // Act
        var result = await _sut.GetPaymentDashboardDataAsync(parameters);

        // Assert
        result.Should().NotBeNull();
        result.TodayAnalytics.TotalPayments.Should().Be(5);
        result.Last7DaysAnalytics.TotalPayments.Should().Be(50);

        _mockCacheService.Verify(c => c.Get<PaymentDashboardDto>(It.IsAny<string>()), Times.Once);
        _mockPaymentRepository.Verify(r => r.GetTodayAnalyticsAsync(), Times.Never);
        _mockPaymentRepository.Verify(r => r.GetLast7DaysAnalyticsAsync(), Times.Never);
        _mockPaymentRepository.Verify(r => r.GetLast30DaysAnalyticsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetPaymentDashboardDataAsync_ShouldFetchFromDatabase_WhenCacheMiss()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };
        var analytics = new PaymentAnalyticsDto { TotalPayments = 10, TotalRevenue = 1000m };

        _mockCacheService.Setup(c => c.Get<PaymentDashboardDto>(It.IsAny<string>()))
            .Returns((PaymentDashboardDto?)null);
        _mockCacheService.Setup(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.IsAny<string>()))
            .Returns((PaymentPaginatedResult<PaymentDto>?)null);
        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync([]);
        _mockPaymentRepository.Setup(r => r.GetTodayAnalyticsAsync()).ReturnsAsync(analytics);
        _mockPaymentRepository.Setup(r => r.GetLast7DaysAnalyticsAsync()).ReturnsAsync(analytics);
        _mockPaymentRepository.Setup(r => r.GetLast30DaysAnalyticsAsync()).ReturnsAsync(analytics);

        // Act
        var result = await _sut.GetPaymentDashboardDataAsync(parameters);

        // Assert
        result.Should().NotBeNull();

        _mockPaymentRepository.Verify(r => r.GetTodayAnalyticsAsync(), Times.Once);
        _mockPaymentRepository.Verify(r => r.GetLast7DaysAnalyticsAsync(), Times.Once);
        _mockPaymentRepository.Verify(r => r.GetLast30DaysAnalyticsAsync(), Times.Once);
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<PaymentDashboardDto>(), 
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    [Fact]
    public async Task GetPaymentDashboardDataAsync_ShouldCacheResult_WithCorrectTTL()
    {
        // Arrange
        var parameters = new PaymentPaginationParameters { PageNumber = 1, PageSize = 10 };
        var analytics = new PaymentAnalyticsDto { TotalPayments = 10 };

        _mockCacheService.Setup(c => c.Get<PaymentDashboardDto>(It.IsAny<string>()))
            .Returns((PaymentDashboardDto?)null);
        _mockCacheService.Setup(c => c.Get<PaymentPaginatedResult<PaymentDto>>(It.IsAny<string>()))
            .Returns((PaymentPaginatedResult<PaymentDto>?)null);
        _mockPaymentRepository.Setup(r => r.GetPaymentsPagedAsync(It.IsAny<PaymentPaginationParameters>()))
            .ReturnsAsync([]);
        _mockPaymentRepository.Setup(r => r.GetTodayAnalyticsAsync()).ReturnsAsync(analytics);
        _mockPaymentRepository.Setup(r => r.GetLast7DaysAnalyticsAsync()).ReturnsAsync(analytics);
        _mockPaymentRepository.Setup(r => r.GetLast30DaysAnalyticsAsync()).ReturnsAsync(analytics);

        // Act
        await _sut.GetPaymentDashboardDataAsync(parameters);

        // Assert
        _mockCacheService.Verify(c => c.Set(
            It.IsAny<string>(), 
            It.IsAny<PaymentDashboardDto>(), 
            TimeSpan.FromMinutes(3)), Times.Once);
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public async Task CreatePaymentAsync_ShouldInvalidateAllRelevantCaches()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();

        _mockPaymentRepository.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Dashboard:"), Times.Once);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ShouldInvalidateSpecificAndGlobalCaches()
    {
        // Arrange
        var confirmDto = CreateValidConfirmPaymentDto();
        var payment = CreatePayment(confirmDto.PaymentId, confirmDto.Amount);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(confirmDto.PaymentId)).ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.ConfirmPaymentAsync(confirmDto);

        // Assert
        _mockCacheService.Verify(c => c.Remove($"Payment:Details:{confirmDto.PaymentId}"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Dashboard:"), Times.Once);
    }

    [Fact]
    public async Task FailPaymentAsync_ShouldInvalidateSpecificAndGlobalCaches()
    {
        // Arrange
        int paymentId = 1;
        var payment = CreatePayment(paymentId, 100m, PaymentStatus.Processing, false);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.FailPaymentAsync(paymentId, "txn_123");

        // Assert
        _mockCacheService.Verify(c => c.Remove($"Payment:Details:{paymentId}"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Dashboard:"), Times.Once);
    }

    [Fact]
    public async Task CancelPaymentAsync_ShouldInvalidateSpecificAndGlobalCaches()
    {
        // Arrange
        int paymentId = 1;
        var payment = CreatePayment(paymentId, 100m, PaymentStatus.Processing, false);

        _mockPaymentRepository.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _sut.CancelPaymentAsync(paymentId, "txn_123");

        // Assert
        _mockCacheService.Verify(c => c.Remove($"Payment:Details:{paymentId}"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Paginated:"), Times.Once);
        _mockCacheService.Verify(c => c.RemoveByPrefix("Payment:Dashboard:"), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldNotInvalidateCache_WhenSaveChangesFails()
    {
        // Arrange
        var createPaymentDto = CreateValidPaymentDto();

        _mockPaymentRepository.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _mockPaymentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        await _sut.CreatePaymentAsync(createPaymentDto);

        // Assert
        _mockCacheService.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #endregion

    #region Helper Methods

    private static CreatePaymentDto CreateValidPaymentDto()
    {
        return new CreatePaymentDto
        {
            Provider = PaymentType.Stripe,
            Amount = 100.50m,
            Currency = "USD",
            Status = PaymentStatus.Processing,
            UserId = "user-123",
            OrderId = 1
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

    private static Payment CreatePayment(int id, decimal amount, PaymentStatus status = PaymentStatus.Processing, bool isFulfilled = false)
    {
        return new Payment
        {
            Id = id,
            Amount = amount,
            Status = status,
            IsFulfilled = isFulfilled,
            OrderId = 1,
            UserId = "user-123",
            Currency = "USD",
            Provider = PaymentType.Stripe
        };
    }

    #endregion
}
