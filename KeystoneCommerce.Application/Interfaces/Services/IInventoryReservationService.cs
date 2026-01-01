namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IInventoryReservationService
    {
        Task<Result<string>> CreateReservationAsync(int orderId, PaymentType paymentType);
        Task CheckExpiredReservation(int orderId);
        Task<Result<string>> UpdateReservationStatusToConsumedAsync(int orderId);
    }
}
