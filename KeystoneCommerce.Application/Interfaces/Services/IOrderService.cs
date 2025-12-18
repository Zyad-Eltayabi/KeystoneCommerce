using KeystoneCommerce.Application.Common.Result_Pattern;
using KeystoneCommerce.Application.DTOs.Checkout;
using KeystoneCommerce.Application.DTOs.Order;

namespace KeystoneCommerce.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<Result<OrderDto>> CreateNewOrder(CreateOrderDto createOrderDto);
        Task<Result<bool>> UpdateOrderPaymentStatus(int orderId);
        Task<Result<string>> UpdateOrderStatusToFailed(int orderId);
        Task<Result<string>> UpdateOrderStatusToCancelled(int orderId);
        Task<bool> ReleaseReservedStock(int orderId);
    }
}
