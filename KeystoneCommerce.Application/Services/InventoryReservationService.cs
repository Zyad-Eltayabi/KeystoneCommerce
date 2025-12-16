using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.Common.Settings;
using KeystoneCommerce.Application.Interfaces.Repositories;
using KeystoneCommerce.Application.Interfaces.Services;
using KeystoneCommerce.Domain.Entities;
using KeystoneCommerce.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeystoneCommerce.Application.Services
{
    public class InventoryReservationService : IInventoryReservationService
    {
        private readonly IInventoryReservationRepository _inventoryReservationRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<InventoryReservationService> _logger;
        private readonly InventorySettings _inventorySettings;

        public InventoryReservationService(
            IInventoryReservationRepository inventoryReservationRepository,
            IOrderRepository orderRepository,
            ILogger<InventoryReservationService> logger,
            IOptions<InventorySettings> inventorySettings)
        {
            _inventoryReservationRepository = inventoryReservationRepository;
            _orderRepository = orderRepository;
            _logger = logger;
            _inventorySettings = inventorySettings.Value;
        }

        public async Task<Result<string>> CreateReservationAsync(int orderId)
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
                ExpiresAt = DateTime.UtcNow.AddMinutes(_inventorySettings.ReservationExpirationMinutes),
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
            return Result<string>.Success("Reservation created successfully");
        }
    }
}
