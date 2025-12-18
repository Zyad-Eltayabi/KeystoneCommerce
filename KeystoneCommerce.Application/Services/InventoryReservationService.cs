using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.Common.Settings;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeystoneCommerce.Application.Services;

public class InventoryReservationService : IInventoryReservationService
{
    private readonly IInventoryReservationRepository _inventoryReservationRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<InventoryReservationService> _logger;
    private readonly InventorySettings _inventorySettings;
    private readonly IBackgroundService _backgroundService;
    private IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryReservationService(
        IInventoryReservationRepository inventoryReservationRepository,
        IOrderRepository orderRepository,
        ILogger<InventoryReservationService> logger,
        IOptions<InventorySettings> inventorySettings,
        IBackgroundService backgroundService,
        IOrderService orderService,
        IUnitOfWork unitOfWork)
    {
        _inventoryReservationRepository = inventoryReservationRepository;
        _orderRepository = orderRepository;
        _logger = logger;
        _inventorySettings = inventorySettings.Value;
        _backgroundService = backgroundService;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> CreateReservationAsync(int orderId, PaymentType paymentType)
    {
        _logger.LogInformation("Creating inventory reservation for order: {OrderId}", orderId);

        if (!await _orderRepository.ExistsAsync(o => o.Id == orderId))
        {
            _logger.LogWarning("Order with ID {OrderId} does not exist", orderId);
            return Result<string>.Failure("Order does not exist");
        }

        var reservation = new InventoryReservation
        {
            OrderId = orderId,
            ExpiresAt = paymentType == PaymentType.CashOnDelivery ? null
            : DateTime.UtcNow.AddMinutes(_inventorySettings.ReservationExpirationMinutes),
            Status = ReservationStatus.Active
        };

        await _inventoryReservationRepository.AddAsync(reservation);
        var result = await _inventoryReservationRepository.SaveChangesAsync();

        if (result == 0)
        {
            _logger.LogError("Failed to create inventory reservation for order: {OrderId}", orderId);
            return Result<string>.Failure("Failed to create reservation");
        }

        _logger.LogInformation("Inventory reservation created successfully for order: {OrderId}", orderId);

        if (paymentType != PaymentType.CashOnDelivery && reservation.ExpiresAt.HasValue)
        {
            _backgroundService.ScheduleJob<IInventoryReservationService>(svc => svc.CheckExpiredReservation(orderId),
                 (reservation.ExpiresAt - DateTime.UtcNow).Value);
        }

        return Result<string>.Success("Reservation created successfully");
    }

    public async Task CheckExpiredReservation(int orderId)
    {
        var reservation = await _inventoryReservationRepository.FindAsync(ir => ir.OrderId == orderId);
        bool flowControl = ValidateReservation(orderId, reservation);
        if (!flowControl)
            return;

        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var stockReleaseResult = await _orderService.ReleaseReservedStock(orderId);
            if (!stockReleaseResult)
            {
                _logger.LogError("Failed to release reserved stock for order: {OrderId}", orderId);
                await _unitOfWork.RollbackAsync();
                return;
            }
            await UpdateReservationStatusToReleasedAsync(orderId, reservation!);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while checking expired reservation for order: {OrderId}", orderId);
            await _unitOfWork.RollbackAsync();
        }
    }

    private async Task UpdateReservationStatusToReleasedAsync(int orderId, InventoryReservation reservation)
    {
        reservation!.Status = ReservationStatus.Released;
        _inventoryReservationRepository.Update(reservation);
        await _inventoryReservationRepository.SaveChangesAsync();
        await _unitOfWork.CommitAsync();
        _logger.LogInformation("Reservation for order: {OrderId} has expired and is now released", orderId);
    }

    private bool ValidateReservation(int orderId, InventoryReservation? reservation)
    {
        if (reservation == null)
        {
            _logger.LogError("No reservation found for order: {OrderId}", orderId);
            return false;
        }

        if (reservation.Status == ReservationStatus.Released)
        {
            _logger.LogInformation("Reservation for order: {OrderId} is already released", orderId);
            return false;
        }

        if (reservation.Status == ReservationStatus.Consumed)
        {
            _logger.LogInformation("Reservation for order: {OrderId} is already consumed", orderId);
            return false;
        }
        return true;
    }
}
