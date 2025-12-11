using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Order;
using KeystoneCommerce.Application.DTOs.Payment;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace KeystoneCommerce.Application.Services;

public class CheckoutService : ICheckoutService
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(IOrderService orderService, IPaymentRepository paymentRepository, IUnitOfWork unitOfWork, ILogger<CheckoutService> logger, IPaymentService paymentService)
    {
        _orderService = orderService;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _paymentService = paymentService;
    }

    public async Task<Result<OrderDto>> SubmitOrder(CreateOrderDto order)
    {
        try
        {

            if (order.ProductsWithQuantity == null || !order.ProductsWithQuantity.Any())
            {
                return Result<OrderDto>.Failure("The order must contain at least one product.");
            }

            if (!Enum.TryParse<PaymentType>(order.PaymentProvider, out var paymentType))
            {
                _logger.LogWarning("Invalid payment type provided: {PaymentType}", order.PaymentProvider);
                return Result<OrderDto>.Failure("Invalid payment type.");
            }

            await _unitOfWork.BeginTransactionAsync();

            var orderCreationResult = await _orderService.CreateNewOrder(order);
            if (!orderCreationResult.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return Result<OrderDto>.Failure(orderCreationResult.Errors);
            }

            var orderData = orderCreationResult.Data!;
            var paymentDto = new CreatePaymentDto
            {
                OrderId = orderData.Id,
                Amount = orderData.Total,
                Provider = paymentType,
                UserId = order.UserId,
                Currency = orderData.Currency,
                Status = PaymentStatus.Pending,
            };

            var paymentResult = await _paymentService.CreatePaymentAsync(paymentDto);
            if (!paymentResult.IsSuccess)
            {
                await _unitOfWork.RollbackAsync();
                return Result<OrderDto>.Failure(paymentResult.Errors);
            }

            await _unitOfWork.CommitAsync();
            return Result<OrderDto>.Success(orderCreationResult.Data);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "An error occurred while submitting the order.");
            return Result<OrderDto>.Failure("An unexpected error occurred while processing your order. Please try again later.");
        }
    }
}
