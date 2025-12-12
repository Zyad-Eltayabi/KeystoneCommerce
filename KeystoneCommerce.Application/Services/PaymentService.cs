using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IApplicationValidator<CreatePaymentDto> _validator;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IMappingService _mappingService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IApplicationValidator<CreatePaymentDto> validator,
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IMappingService mappingService,
            ILogger<PaymentService> logger)
        {
            _validator = validator;
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _mappingService = mappingService;
            _logger = logger;
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

            return Result<bool>.Success();
        }
    }
}
