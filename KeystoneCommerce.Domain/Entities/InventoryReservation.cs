using KeystoneCommerce.Domain.Enums;

namespace KeystoneCommerce.Domain.Entities;

public class InventoryReservation
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public ReservationStatus Status { get; set; }
    public Order Order { get; set; } = null!;
}
