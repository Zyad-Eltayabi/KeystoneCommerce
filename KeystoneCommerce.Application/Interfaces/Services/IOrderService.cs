using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<Result<OrderDto>> CreateNewOrder(CreateOrderDto createOrderDto);
        Task<Result<bool>> MarkOrderAsPaid(int orderId);
        Task<Result<string>> UpdateOrderStatusToFailed(int orderId);
        Task<Result<string>> UpdateOrderStatusToCancelled(int orderId);
        Task<bool> ReleaseReservedStock(int orderId);
        Task<string> GetOrderNumberByPaymentId(int paymentId);
        Task<OrderPaginatedResult<OrderDto>> GetAllOrdersPaginatedAsync(OrderPaginationParameters parameters);
        Task<Result<OrderDetailsDto>> GetOrderDetailsByIdAsync(int orderId);
    }
}
