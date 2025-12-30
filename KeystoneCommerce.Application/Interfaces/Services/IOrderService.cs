using KeystoneCommerce.Application.Common.Pagination;
using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Checkout;
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
        Task<PaginatedResult<OrderDto>> GetAllOrdersPaginatedAsync(PaginationParameters parameters);
    }
}
