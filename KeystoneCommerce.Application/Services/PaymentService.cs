using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IApplicationValidator<CreatePaymentDto> _validator;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMappingService _mappingService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IApplicationValidator<CreatePaymentDto> validator,
            IPaymentRepository paymentRepository,
            IMappingService mappingService,
            ILogger<PaymentService> logger)
        {
            _validator = validator;
            _paymentRepository = paymentRepository;
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
    }
}
