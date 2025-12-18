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
    private readonly IInventoryReservationService _inventoryReservationService;

    public PaymentGatewayService(
        IStripPaymentService stripPaymentService, 
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        IOrderService orderService,
        IUnitOfWork unitOfWork,
        ILogger<PaymentGatewayService> logger,
        IInventoryReservationService inventoryReservationService)
    {
        _stripPaymentService = stripPaymentService;
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _inventoryReservationService = inventoryReservationService;
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
            var updateOrderResult = await _orderService.MarkOrderAsPaid(orderId.Value);
            if (!updateOrderResult.IsSuccess)
            {
                _logger.LogError("Failed to update order status for Order ID: {OrderId} after successful payment. Payment ID: {PaymentId}. Errors: {Errors}",
                    orderId.Value, confirmPaymentDto.PaymentId, string.Join(", ", updateOrderResult.Errors));
                await _unitOfWork.RollbackAsync();
                return Result<bool>.Failure(updateOrderResult.Errors);
            }

            // Step 4: Update inventory reservation status to consumed
            var updateReservationResult = await _inventoryReservationService.UpdateReservationStatusToConsumedAsync(orderId.Value);
            if (!updateReservationResult.IsSuccess)
            {
                _logger.LogError("Failed to update inventory reservation status to consumed for Order ID: {OrderId} after successful payment. Payment ID: {PaymentId}. Errors: {Errors}",
                    orderId.Value, confirmPaymentDto.PaymentId, string.Join(", ", updateReservationResult.Errors));
                await _unitOfWork.RollbackAsync();
                return Result<bool>.Failure(updateReservationResult.Errors);
            }

            _logger.LogInformation("Payment confirmation, order update, and inventory reservation consumed completed successfully. Payment ID: {PaymentId}, Order ID: {OrderId}",
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

    public async Task<Result<string>> FailPaymentAndUpdateOrderAsync(FailPaymentDto failPaymentDto)
    {
        _logger.LogInformation("Starting payment failure process for Payment ID: {PaymentId}",
            failPaymentDto.PaymentId);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Step 1: Mark the payment as failed and retrieve the order ID
            var failPaymentResult = await _paymentService.FailPaymentAsync(
                failPaymentDto.PaymentId,
                failPaymentDto.ProviderTransactionId);

            if (!failPaymentResult.IsSuccess)
            {
                _logger.LogWarning("Payment failure update failed for Payment ID: {PaymentId}. Errors: {Errors}",
                    failPaymentDto.PaymentId, string.Join(", ", failPaymentResult.Errors));
                await _unitOfWork.RollbackAsync();
                return Result<string>.Failure(failPaymentResult.Errors);
            }

            var orderId = failPaymentResult.Data;
            _logger.LogInformation("Payment marked as failed successfully for Payment ID: {PaymentId}, Order ID: {OrderId}",
                failPaymentDto.PaymentId, orderId);

            // Step 2: Update the order status to failed
            var updateOrderResult = await _orderService.UpdateOrderStatusToFailed(orderId);
            if (!updateOrderResult.IsSuccess)
            {
                _logger.LogError("Failed to update order status to failed for Order ID: {OrderId} after payment failure. Payment ID: {PaymentId}. Errors: {Errors}",
                    orderId, failPaymentDto.PaymentId, string.Join(", ", updateOrderResult.Errors));
                await _unitOfWork.RollbackAsync();
                return Result<string>.Failure(updateOrderResult.Errors);
            }

            _logger.LogInformation("Payment and order marked as failed successfully. Payment ID: {PaymentId}, Order ID: {OrderId}",
                failPaymentDto.PaymentId, orderId);

            await _unitOfWork.CommitAsync();
            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during payment failure process. Payment ID: {PaymentId}",
                failPaymentDto.PaymentId);
            await _unitOfWork.RollbackAsync();
            return Result<string>.Failure("An unexpected error occurred during payment failure processing.");
        }
    }

    public async Task<Result<string>> CancelPaymentAndUpdateOrderAsync(CancelPaymentDto cancelPaymentDto)
    {
        _logger.LogInformation("Starting payment cancellation process for Payment ID: {PaymentId}",
            cancelPaymentDto.PaymentId);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Step 1: Mark the payment as cancelled and retrieve the order ID
            var cancelPaymentResult = await _paymentService.CancelPaymentAsync(
                cancelPaymentDto.PaymentId,
                cancelPaymentDto.ProviderTransactionId);

            if (!cancelPaymentResult.IsSuccess)
            {
                _logger.LogWarning("Payment cancellation update failed for Payment ID: {PaymentId}. Errors: {Errors}",
                    cancelPaymentDto.PaymentId, string.Join(", ", cancelPaymentResult.Errors));
                await _unitOfWork.RollbackAsync();
                return Result<string>.Failure(cancelPaymentResult.Errors);
            }

            var orderId = cancelPaymentResult.Data;
            _logger.LogInformation("Payment marked as cancelled successfully for Payment ID: {PaymentId}, Order ID: {OrderId}",
                cancelPaymentDto.PaymentId, orderId);

            // Step 2: Update the order status to cancelled
            var updateOrderResult = await _orderService.UpdateOrderStatusToCancelled(orderId);
            if (!updateOrderResult.IsSuccess)
            {
                _logger.LogError("Failed to update order status to cancelled for Order ID: {OrderId} after payment cancellation. Payment ID: {PaymentId}. Errors: {Errors}",
                    orderId, cancelPaymentDto.PaymentId, string.Join(", ", updateOrderResult.Errors));
                await _unitOfWork.RollbackAsync();
                return Result<string>.Failure(updateOrderResult.Errors);
            }

            _logger.LogInformation("Payment and order marked as cancelled successfully. Payment ID: {PaymentId}, Order ID: {OrderId}",
                cancelPaymentDto.PaymentId, orderId);

            await _unitOfWork.CommitAsync();
            return Result<string>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during payment cancellation process. Payment ID: {PaymentId}",
                cancelPaymentDto.PaymentId);
            await _unitOfWork.RollbackAsync();
            return Result<string>.Failure("An unexpected error occurred during payment cancellation processing.");
        }
    }
}