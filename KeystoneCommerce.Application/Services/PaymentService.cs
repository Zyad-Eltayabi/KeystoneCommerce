using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Shared.Constants;

namespace KeystoneCommerce.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IApplicationValidator<CreatePaymentDto> _validator;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IMappingService _mappingService;
        private readonly ILogger<PaymentService> _logger;
        private readonly IIdentityService _identityService;
        private readonly ICacheService _cacheService;

        public PaymentService(
            IApplicationValidator<CreatePaymentDto> validator,
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IMappingService mappingService,
            ILogger<PaymentService> logger,
            IIdentityService identityService,
            ICacheService cacheService)
        {
            _validator = validator;
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _mappingService = mappingService;
            _logger = logger;
            _identityService = identityService;
            _cacheService = cacheService;
        }

        public async Task<Result<int>> CreatePaymentAsync(CreatePaymentDto createPaymentDto)
        {
            _logger.LogInformation("Creating payment for Order ID: {OrderId}, User ID: {UserId}, Amount: {Amount}",
                createPaymentDto.OrderId, createPaymentDto.UserId, createPaymentDto.Amount);

            var validationResult = _validator.Validate(createPaymentDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Payment validation failed for Order ID: {OrderId}. Errors: {ValidationErrors}",
                    createPaymentDto.OrderId, string.Join(", ", validationResult.Errors));
                return Result<int>.Failure(validationResult.Errors);
            }

            var payment = _mappingService.Map<Payment>(createPaymentDto);

            await _paymentRepository.AddAsync(payment);
            var result = await _paymentRepository.SaveChangesAsync();

            if (result == 0)
            {
                _logger.LogError("Failed to save payment to database. Order ID: {OrderId}",
                    createPaymentDto.OrderId);
                return Result<int>.Failure("Failed to create payment.");
            }

            _logger.LogInformation(
                "Payment created successfully: Payment ID {PaymentId}, Order ID: {OrderId}, Status: {Status}",
                payment.Id, createPaymentDto.OrderId, createPaymentDto.Status);

            // Invalidate cache: new payment affects dashboard and paginated lists
            await InvalidatePaymentCachesAsync();

            return Result<int>.Success(payment.Id);
        }

        public async Task<Result<bool>> ConfirmPaymentAsync(ConfirmPaymentDto confirmPaymentDto)
        {
            _logger.LogInformation("Confirming payment for Payment ID: {PaymentId} with Provider Transaction ID: {ProviderTransactionId}, Amount: {Amount}",
                confirmPaymentDto.PaymentId, confirmPaymentDto.ProviderTransactionId, confirmPaymentDto.Amount);

            var payment = await _paymentRepository.GetByIdAsync(confirmPaymentDto.PaymentId);
            if (payment is null)
            {
                _logger.LogWarning("Payment not found for ID: {PaymentId}", confirmPaymentDto.PaymentId);
                return Result<bool>.Failure("Payment not found.");
            }

            // Verify the amount matches
            if (payment.Amount != confirmPaymentDto.Amount)
            {
                _logger.LogWarning(
                    "Payment amount mismatch for Payment ID: {PaymentId}. Expected: {ExpectedAmount}, Received: {ReceivedAmount}",
                    confirmPaymentDto.PaymentId, payment.Amount, confirmPaymentDto.Amount);
                return Result<bool>.Failure("Payment amount does not match the order amount.");
            }

            // Update payment
            payment.ProviderTransactionId = confirmPaymentDto.ProviderTransactionId;
            payment.Status = PaymentStatus.Successful;
            payment.IsFulfilled = true;
            payment.UpdatedAt = DateTime.UtcNow;

            _paymentRepository.Update(payment);
            var result = await _paymentRepository.SaveChangesAsync();

            if (result == 0)
            {
                _logger.LogError("Failed to confirm payment. Payment ID: {PaymentId}", confirmPaymentDto.PaymentId);
                return Result<bool>.Failure("Failed to confirm payment.");
            }

            _logger.LogInformation(
                "Payment confirmed successfully: Payment ID {PaymentId}, Order ID: {OrderId}, Provider Transaction ID: {ProviderTransactionId}, Payment Status: {PaymentStatus}, Order Status: {OrderStatus}",
                confirmPaymentDto.PaymentId, payment.OrderId, confirmPaymentDto.ProviderTransactionId, 
                PaymentStatus.Successful, OrderStatus.Paid);

            // Invalidate cache: payment status changed
            await InvalidatePaymentCachesAsync(confirmPaymentDto.PaymentId);

            return Result<bool>.Success();
        }

        public async Task<Result<int>> FailPaymentAsync(int paymentId, string providerTransactionId)
        {
            _logger.LogInformation("Marking payment as failed for Payment ID: {PaymentId} with Provider Transaction ID: {ProviderTransactionId}",
                paymentId, providerTransactionId);

            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment is null)
            {
                _logger.LogWarning("Payment not found for ID: {PaymentId}", paymentId);
                return Result<int>.Failure("Payment not found.");
            }

            if (payment.IsFulfilled)
            {
                _logger.LogWarning("Cannot mark payment as failed - Payment ID: {PaymentId} is already fulfilled.", paymentId);
                return Result<int>.Failure("Cannot mark a fulfilled payment as failed.");
            }

            if (payment.Status == PaymentStatus.Successful)
            {
                _logger.LogWarning("Cannot mark payment as failed - Payment ID: {PaymentId} is already successful.", paymentId);
                return Result<int>.Failure("Cannot mark a successful payment as failed.");
            }

            payment.ProviderTransactionId = providerTransactionId;
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;

            _paymentRepository.Update(payment);
            var result = await _paymentRepository.SaveChangesAsync();

            if (result == 0)
            {
                _logger.LogError("Failed to update payment status to failed. Payment ID: {PaymentId}", paymentId);
                return Result<int>.Failure("Failed to update payment status.");
            }

            _logger.LogInformation("Payment marked as failed successfully. Payment ID: {PaymentId}, Order ID: {OrderId}, Provider Transaction ID: {ProviderTransactionId}",
                paymentId, payment.OrderId, providerTransactionId);

            // Invalidate cache: payment status changed
            await InvalidatePaymentCachesAsync(paymentId);

            return Result<int>.Success(payment.OrderId);
        }

        public async Task<Result<int>> CancelPaymentAsync(int paymentId, string providerTransactionId)
        {
            _logger.LogInformation("Marking payment as cancelled for Payment ID: {PaymentId} with Provider Transaction ID: {ProviderTransactionId}",
                paymentId, providerTransactionId);

            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment is null)
            {
                _logger.LogWarning("Payment not found for ID: {PaymentId}", paymentId);
                return Result<int>.Failure("Payment not found.");
            }

            if (payment.IsFulfilled)
            {
                _logger.LogWarning("Cannot cancel payment - Payment ID: {PaymentId} is already fulfilled.", paymentId);
                return Result<int>.Failure("Cannot cancel a fulfilled payment.");
            }

            if (payment.Status == PaymentStatus.Canceled)
            {
                _logger.LogWarning("Payment ID: {PaymentId} is already cancelled.", paymentId);
                return Result<int>.Failure("Payment is already cancelled.");
            }

            payment.ProviderTransactionId = providerTransactionId;
            payment.Status = PaymentStatus.Canceled;
            payment.UpdatedAt = DateTime.UtcNow;
            _paymentRepository.Update(payment);
            var result = await _paymentRepository.SaveChangesAsync();

            if (result == 0)
            {
                _logger.LogError("Failed to update payment status to cancelled. Payment ID: {PaymentId}", paymentId);
                return Result<int>.Failure("Failed to update payment status.");
            }

            _logger.LogInformation("Payment marked as cancelled successfully. Payment ID: {PaymentId}, Order ID: {OrderId}, Provider Transaction ID: {ProviderTransactionId}",
                paymentId, payment.OrderId, providerTransactionId);

            // Invalidate cache: payment status changed
            await InvalidatePaymentCachesAsync(paymentId);

            return Result<int>.Success(payment.OrderId);
        }

        public async Task<PaymentPaginatedResult<PaymentDto>> GetAllPaymentsPaginatedAsync(PaymentPaginationParameters parameters)
        {
            _logger.LogInformation("Fetching paginated payments. PageNumber: {PageNumber}, PageSize: {PageSize}, Status: {Status}, Provider: {Provider}", 
                parameters.PageNumber, parameters.PageSize, parameters.Status, parameters.Provider);

            if (string.IsNullOrEmpty(parameters.SortBy))
            {
                parameters.SortBy = "CreatedAt";
                parameters.SortOrder = Sorting.Descending;
            }

            // Cache key includes all pagination parameters to prevent collisions
            var cacheKey = $"Payment:Paginated:Page{parameters.PageNumber}:Size{parameters.PageSize}:Sort{parameters.SortBy}:{parameters.SortOrder}:Search{parameters.SearchBy}:{parameters.SearchValue}:Status{parameters.Status}:Provider{parameters.Provider}";

            var cachedResult = _cacheService.Get<PaymentPaginatedResult<PaymentDto>>(cacheKey);
            if (cachedResult is not null)
            {
                _logger.LogInformation("Retrieved {Count} payments from cache", cachedResult.Items.Count);
                return cachedResult;
            }

            var payments = await _paymentRepository.GetPaymentsPagedAsync(parameters);
            var paymentDtos = _mappingService.Map<List<PaymentDto>>(payments);

            _logger.LogInformation("Retrieved {Count} payments successfully from database", paymentDtos.Count);

            var result = new PaymentPaginatedResult<PaymentDto>
            {
                Items = paymentDtos,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalCount = parameters.TotalCount,
                SortBy = parameters.SortBy,
                SortOrder = parameters.SortOrder,
                SearchBy = parameters.SearchBy,
                SearchValue = parameters.SearchValue,
                Status = parameters.Status,
                Provider = parameters.Provider
            };

            // Cache for 5 minutes - semi-dynamic data with moderate volatility
            _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }

        public async Task<Result<PaymentDetailsDto>> GetPaymentDetailsByIdAsync(int paymentId)
        {
            _logger.LogInformation("Fetching payment details for payment ID: {PaymentId}", paymentId);

            var cacheKey = $"Payment:Details:{paymentId}";

            var cachedPaymentDetails = _cacheService.Get<PaymentDetailsDto>(cacheKey);
            if (cachedPaymentDetails is not null)
            {
                _logger.LogInformation("Successfully retrieved payment details for payment ID: {PaymentId} from cache", paymentId);
                return Result<PaymentDetailsDto>.Success(cachedPaymentDetails);
            }

            var payment = await _paymentRepository.GetPaymentDetailsByIdAsync(paymentId);
            if (payment is null)
            {
                _logger.LogWarning("Payment not found with ID: {PaymentId}", paymentId);
                return Result<PaymentDetailsDto>.Failure("Payment not found.");
            }

            var userInfo = await _identityService.GetUserBasicInfoByIdAsync(payment.UserId);
            if (userInfo is null)
            {
                _logger.LogWarning("User not found for payment ID: {PaymentId}, User ID: {UserId}", paymentId, payment.UserId);
                return Result<PaymentDetailsDto>.Failure("User information not found.");
            }

            var paymentDetailsDto = _mappingService.Map<PaymentDetailsDto>(payment);
            paymentDetailsDto.User = userInfo;

            // Cache for 10 minutes with sliding expiration - payment details stabilize after fulfillment
            _cacheService.Set(cacheKey, paymentDetailsDto, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5));

            _logger.LogInformation("Successfully retrieved payment details for payment ID: {PaymentId} from database", paymentId);
            return Result<PaymentDetailsDto>.Success(paymentDetailsDto);
        }

        public async Task<PaymentDashboardDto> GetPaymentDashboardDataAsync(PaymentPaginationParameters parameters)
        {
            _logger.LogInformation("Fetching payment dashboard data. PageNumber: {PageNumber}, PageSize: {PageSize}, Status: {Status}, Provider: {Provider}", 
                parameters.PageNumber, parameters.PageSize, parameters.Status, parameters.Provider);

            // Cache key includes all parameters to prevent collisions
            var cacheKey = $"Payment:Dashboard:Page{parameters.PageNumber}:Size{parameters.PageSize}:Sort{parameters.SortBy}:{parameters.SortOrder}:Search{parameters.SearchBy}:{parameters.SearchValue}:Status{parameters.Status}:Provider{parameters.Provider}";

            var cachedDashboard = _cacheService.Get<PaymentDashboardDto>(cacheKey);
            if (cachedDashboard is not null)
            {
                _logger.LogInformation("Successfully retrieved payment dashboard data from cache");
                return cachedDashboard;
            }

            var paginatedPayments = await GetAllPaymentsPaginatedAsync(parameters);
            var todayAnalytics = await _paymentRepository.GetTodayAnalyticsAsync();
            var last7DaysAnalytics = await _paymentRepository.GetLast7DaysAnalyticsAsync();
            var last30DaysAnalytics = await _paymentRepository.GetLast30DaysAnalyticsAsync();

            var dashboardData = new PaymentDashboardDto
            {
                PaginatedPayments = paginatedPayments,
                TodayAnalytics = todayAnalytics,
                Last7DaysAnalytics = last7DaysAnalytics,
                Last30DaysAnalytics = last30DaysAnalytics
            };

            // Cache for 3 minutes - includes real-time analytics requiring fresher data
            _cacheService.Set(cacheKey, dashboardData, TimeSpan.FromMinutes(3));

            _logger.LogInformation("Successfully retrieved payment dashboard data from database");

            return dashboardData;
        }

        // Helper method to invalidate all payment-related caches
        private Task InvalidatePaymentCachesAsync(int? paymentId = null)
        {
            // Invalidate specific payment details cache if paymentId provided
            if (paymentId.HasValue)
            {
                var detailsCacheKey = $"Payment:Details:{paymentId.Value}";
                _cacheService.Remove(detailsCacheKey);
                _logger.LogInformation("Invalidated cache for payment ID: {PaymentId}", paymentId.Value);
            }

            // Invalidate all paginated and dashboard caches using pattern matching
            _cacheService.RemoveByPrefix("Payment:Paginated:");
            _cacheService.RemoveByPrefix("Payment:Dashboard:");

            _logger.LogInformation("Invalidated all payment paginated and dashboard caches");

            return Task.CompletedTask;
        }
    }
}
