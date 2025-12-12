using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services;

public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly IStripPaymentService _stripPaymentService;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentGatewayService> _logger;

    public PaymentGatewayService(
        IStripPaymentService stripPaymentService, 
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        IOrderService orderService,
        IUnitOfWork unitOfWork,
        ILogger<PaymentGatewayService> logger)
    {
        _stripPaymentService = stripPaymentService;
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PaymentSessionResultDto>> CreatePaymentSessionAsync(CreatePaymentSessionDto sessionDto)
    {
        _logger.LogInformation("Creating payment session for PaymentId: {PaymentId}", sessionDto.PaymentId);
        try
        {
            var result = await _stripPaymentService.CreatePaymentSessionAsync(sessionDto);
            return Result<PaymentSessionResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment session for PaymentId: {PaymentId}", sessionDto.PaymentId);
            return Result<PaymentSessionResultDto>.Failure("Failed to create payment session.");
        }
    }

    public async Task<Result<bool>> ConfirmPaymentAndUpdateOrderAsync(ConfirmPaymentDto confirmPaymentDto)
    {
        _logger.LogInformation("Starting payment confirmation process for Payment ID: {PaymentId}", 
            confirmPaymentDto.PaymentId);

        // Check if payment is already fulfilled (duplicate request)
        var isFulfilled = await _paymentRepository.IsPaymentFulfilledAsync(confirmPaymentDto.PaymentId);
        if (isFulfilled)
        {
            _logger.LogInformation("Payment ID: {PaymentId} is already fulfilled. Skipping duplicate request.", 
                confirmPaymentDto.PaymentId);
            return Result<bool>.Success();
        }

        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Step 1: Confirm the payment
            var confirmPaymentResult = await _paymentService.ConfirmPaymentAsync(confirmPaymentDto);
            if (!confirmPaymentResult.IsSuccess)
            {
                _logger.LogWarning("Payment confirmation failed for Payment ID: {PaymentId}. Errors: {Errors}",
                    confirmPaymentDto.PaymentId, string.Join(", ", confirmPaymentResult.Errors));
                await _unitOfWork.RollbackAsync();
                return Result<bool>.Failure(confirmPaymentResult.Errors);
            }

            _logger.LogInformation("Payment confirmed successfully for Payment ID: {PaymentId}", 
                confirmPaymentDto.PaymentId);

            // Step 2: Get the OrderId from the payment
            var orderId = await _paymentRepository.GetOrderIdByPaymentIdAsync(confirmPaymentDto.PaymentId);
            if (orderId is null)
            {
                _logger.LogError("Failed to retrieve order ID after payment confirmation. Payment ID: {PaymentId}", 
                    confirmPaymentDto.PaymentId);
                await _unitOfWork.RollbackAsync();
                return Result<bool>.Failure("Failed to retrieve order details.");
            }

            // Step 3: Update the order status
            var updateOrderResult = await _orderService.UpdateOrderPaymentStatus(orderId.Value);
            if (!updateOrderResult.IsSuccess)
            {
                _logger.LogError("Failed to update order status for Order ID: {OrderId} after successful payment. Payment ID: {PaymentId}. Errors: {Errors}",
                    orderId.Value, confirmPaymentDto.PaymentId, string.Join(", ", updateOrderResult.Errors));
                await _unitOfWork.RollbackAsync();
                return Result<bool>.Failure(updateOrderResult.Errors);
            }

            _logger.LogInformation("Payment confirmation and order update completed successfully. Payment ID: {PaymentId}, Order ID: {OrderId}",
                confirmPaymentDto.PaymentId, orderId.Value);

            await _unitOfWork.CommitAsync();
            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during payment confirmation and order update. Payment ID: {PaymentId}", 
                confirmPaymentDto.PaymentId);
            await _unitOfWork.RollbackAsync();
            return Result<bool>.Failure("An unexpected error occurred during payment processing.");
        }
    }
}